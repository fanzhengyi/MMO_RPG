using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>验证移动状态、连招窗口、冷却及中断行为。</summary>
public sealed class PlayerFsmTests
{
    private PlayerSettings settings;
    private PlayerFsmCore fsm;

    /// <summary>使用一秒动作方便检查时间窗口。</summary>
    [SetUp]
    public void SetUp()
    {
        settings = new PlayerSettings();
        foreach (var attack in settings.Attacks) attack.Duration = 1f;
        foreach (var skill in settings.Skills) skill.Duration = 1f;
        fsm = new PlayerFsmCore(settings);
    }

    /// <summary>方向和 Shift 决定待机、移动及奔跑。</summary>
    [Test]
    public void MovementUsesDirectionAndRunHold()
    {
        Tick(new PlayerInputFrame { IsRun = true });
        Assert.AreEqual(PlayerStateType.Idle, fsm.State);
        Tick(new PlayerInputFrame { Move = Vector2.up });
        Assert.AreEqual(PlayerStateType.Move, fsm.State);
        Assert.AreEqual(settings.WalkSpeed, fsm.MovementSpeed);
        Tick(new PlayerInputFrame { Move = Vector2.up, IsRun = true });
        Assert.AreEqual(PlayerStateType.Run, fsm.State);
        Assert.AreEqual(settings.RunSpeed, fsm.MovementSpeed);
        Tick(new PlayerInputFrame { Move = Vector2.up });
        Assert.AreEqual(PlayerStateType.Move, fsm.State);
        Tick(default);
        Assert.AreEqual(PlayerStateType.Idle, fsm.State);
    }

    /// <summary>起跳一次，经过最高点下落，落地恢复奔跑。</summary>
    [Test]
    public void JumpDoesNotAllowDoubleJumpOrAirAttack()
    {
        Tick(new PlayerInputFrame { JumpPressed = true });
        Assert.IsTrue(fsm.JumpRequested);
        fsm.Tick(new PlayerInputFrame { JumpPressed = true, AttackPressed = true }, 0.1f, false, 3f);
        Assert.IsFalse(fsm.JumpRequested);
        Assert.AreEqual(PlayerStateType.Jump, fsm.State);
        fsm.Tick(default, 0.1f, false, 0f);
        Assert.AreEqual(PlayerStateType.Fall, fsm.State);
        Tick(new PlayerInputFrame { Move = Vector2.up, IsRun = true });
        Assert.AreEqual(PlayerStateType.Run, fsm.State);
    }

    /// <summary>走出平台只下落，不会凭空起跳。</summary>
    [Test]
    public void LeavingLedgeDoesNotApplyJumpImpulse()
    {
        fsm.Tick(new PlayerInputFrame { JumpPressed = true }, 0.1f, false, -1f);
        Assert.AreEqual(PlayerStateType.Fall, fsm.State);
        Assert.IsFalse(fsm.JumpRequested);
    }

    /// <summary>同帧多个操作按跳跃、技能、普攻排序。</summary>
    [Test]
    public void GroundActionsHaveExplicitPriority()
    {
        Tick(new PlayerInputFrame { JumpPressed = true, SkillIndex = 2, AttackPressed = true });
        Assert.AreEqual(PlayerStateType.Jump, fsm.State);
        fsm.Reset();
        Tick(new PlayerInputFrame { SkillIndex = 2, AttackPressed = true });
        Assert.AreEqual(PlayerStateType.Skill, fsm.State);
    }

    /// <summary>四段连招结束后，下次点击必须从第一段开始。</summary>
    [Test]
    public void FourStepComboFinishesAndResets()
    {
        var started = new List<int>();
        fsm.ActionStarted += (_, index) => started.Add(index);
        Tick(new PlayerInputFrame { AttackPressed = true });
        for (var i = 1; i <= 4; i++)
        {
            Assert.AreEqual(i, fsm.CurrentAttackStep);
            Tick(new PlayerInputFrame { AttackPressed = true }, 0.3f);
            Tick(default, 0.51f);
        }
        Assert.AreEqual(4, fsm.CurrentAttackStep);
        Tick(default, 0.2f);
        Assert.AreEqual(PlayerStateType.Idle, fsm.State);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, started);
        Tick(new PlayerInputFrame { AttackPressed = true });
        Assert.AreEqual(1, fsm.CurrentAttackStep);
    }

    /// <summary>过早、过晚和掉帧后才发生的点击不预约下一段。</summary>
    [TestCase(0.1f)]
    [TestCase(0.71f)]
    [TestCase(1.1f)]
    public void ClickOutsideWindowDoesNotChain(float clickTime)
    {
        var starts = 0;
        fsm.ActionStarted += (_, __) => starts++;
        Tick(new PlayerInputFrame { AttackPressed = true });
        Tick(new PlayerInputFrame { AttackPressed = true }, clickTime);
        Tick(default, 1.1f);
        Assert.AreEqual(1, starts);
        Assert.AreEqual(PlayerStateType.Idle, fsm.State);
    }

    /// <summary>一次点击或一直按住都不会自动完成连招。</summary>
    [Test]
    public void OneClickDoesNotAutomaticallyChain()
    {
        var starts = 0;
        fsm.ActionStarted += (_, __) => starts++;
        Tick(new PlayerInputFrame { AttackPressed = true });
        for (var i = 0; i < 20; i++) Tick(default, 0.1f);
        Assert.AreEqual(1, starts);
        Assert.AreEqual(0, fsm.CurrentAttackStep);
    }

    /// <summary>窗口内连点只预约一段，不会将多余点击带到第三段。</summary>
    [Test]
    public void MashBuffersOnlyOneNextStep()
    {
        Tick(new PlayerInputFrame { AttackPressed = true });
        Tick(new PlayerInputFrame { AttackPressed = true }, 0.3f);
        Tick(new PlayerInputFrame { AttackPressed = true }, 0.1f);
        Tick(new PlayerInputFrame { AttackPressed = true }, 0.1f);
        Tick(default, 0.31f);
        Assert.AreEqual(2, fsm.CurrentAttackStep);
        Tick(default, 1.1f);
        Assert.AreEqual(PlayerStateType.Idle, fsm.State);
    }

    /// <summary>跨过表现点仍只触发一次事件。</summary>
    [Test]
    public void ImpactIsEmittedOnceAcrossLargeFrame()
    {
        var hits = 0;
        fsm.ActionImpact += (_, __) => hits++;
        Tick(new PlayerInputFrame { AttackPressed = true });
        Tick(default, 0.6f);
        Tick(default, 0.2f);
        Tick(default, 0.3f);
        Assert.AreEqual(1, hits);
    }

    /// <summary>死亡清除预约，并阻止后续表现事件和跳跃。</summary>
    [Test]
    public void DeathInterruptsQueuedAttack()
    {
        var hits = 0;
        fsm.ActionImpact += (_, __) => hits++;
        Tick(new PlayerInputFrame { AttackPressed = true });
        Tick(new PlayerInputFrame { AttackPressed = true }, 0.3f);
        fsm.SetDead();
        Tick(new PlayerInputFrame { AttackPressed = true, JumpPressed = true }, 2f);
        Assert.AreEqual(PlayerStateType.Dead, fsm.State);
        Assert.AreEqual(0, fsm.CurrentAttackStep);
        Assert.AreEqual(0, hits);
        Assert.IsFalse(fsm.JumpRequested);
        Assert.IsTrue(fsm.BlocksMovement);
    }

    /// <summary>回调中死亡也要终止当帧后续连招判断。</summary>
    [Test]
    public void DeathInsideImpactCallbackStopsAttack()
    {
        fsm.ActionImpact += (_, __) => fsm.SetDead();
        Tick(new PlayerInputFrame { AttackPressed = true });
        Tick(new PlayerInputFrame { AttackPressed = true }, 0.3f);
        Tick(default, 0.6f);
        Assert.AreEqual(PlayerStateType.Dead, fsm.State);
    }

    /// <summary>四个技能各自施放和结束，冷却期间不能重放。</summary>
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void SkillCompletesAndRespectsCooldown(int index)
    {
        Tick(new PlayerInputFrame { SkillIndex = index });
        Assert.AreEqual(PlayerStateType.Skill, fsm.State);
        Assert.AreEqual(index, fsm.CurrentSkillIndex);
        Assert.IsTrue(fsm.BlocksMovement);
        Tick(new PlayerInputFrame { JumpPressed = true, AttackPressed = true }, 0.5f);
        Assert.AreEqual(PlayerStateType.Skill, fsm.State);
        Tick(default, 0.51f);
        Assert.AreEqual(PlayerStateType.Idle, fsm.State);
        Assert.AreEqual(0, fsm.CurrentSkillIndex);
        Tick(new PlayerInputFrame { SkillIndex = index });
        Assert.AreEqual(PlayerStateType.Idle, fsm.State);
        Tick(default, 1f);
        Tick(new PlayerInputFrame { SkillIndex = index });
        Assert.AreEqual(PlayerStateType.Skill, fsm.State);
    }

    /// <summary>离地立即中断地面攻击。</summary>
    [Test]
    public void LosingGroundCancelsCombat()
    {
        Tick(new PlayerInputFrame { AttackPressed = true });
        fsm.Tick(default, 0.1f, false, -2f);
        Assert.AreEqual(PlayerStateType.Fall, fsm.State);
        Assert.AreEqual(0, fsm.CurrentAttackStep);
    }

    /// <summary>暂停不消费输入，复活清除冷却及死亡状态。</summary>
    [Test]
    public void PauseAndResetDoNotLeakState()
    {
        Tick(new PlayerInputFrame { AttackPressed = true }, 0f);
        Assert.AreEqual(PlayerStateType.Idle, fsm.State);
        Tick(new PlayerInputFrame { SkillIndex = 1 });
        fsm.SetDead();
        fsm.Reset();
        Assert.AreEqual(PlayerStateType.Idle, fsm.State);
        Assert.AreEqual(0f, fsm.GetSkillCooldown(1));
        Assert.AreEqual(0, fsm.CurrentSkillIndex);
    }

    /// <summary>模拟角色保持接地的一帧。</summary>
    private void Tick(PlayerInputFrame input, float deltaTime = 0.02f)
    {
        fsm.Tick(input, deltaTime, true, -2f);
    }
}
