using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.TestTools;
using UnityEngine;

/// <summary>验证生成的动画图和真实 Animator 参数切换。</summary>
public sealed class PlayerAnimatorTests
{
    private string path;
    private AnimatorController controller;
    private GameObject model;
    private Animator animator;

    /// <summary>建立临时动画图，并为状态补一秒测试片段。</summary>
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return new EnterPlayMode();
        Assert.IsTrue(Application.isPlaying, "Animator 集成测试必须处于 Play Mode。");
        path = AssetDatabase.GenerateUniqueAssetPath("Assets/PlayerAnimatorTest.controller");
        controller = PlayerAnimatorBuilder.Build(path);
        var clip = new AnimationClip { name = "TestPose" };
        clip.SetCurve("", typeof(Transform), "m_LocalPosition.x", AnimationCurve.Constant(0f, 1f, 0f));
        AssetDatabase.AddObjectToAsset(clip, controller);
        foreach (var state in controller.layers[0].stateMachine.states)
            state.state.motion = clip;
        AssetDatabase.SaveAssets();

        model = new GameObject("PlayerAnimatorTest");
        animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.Rebind();
        animator.Update(0f);
    }

    /// <summary>删除测试对象和临时生成的动画资产。</summary>
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(model);
        AssetDatabase.DeleteAsset(path);
        yield return new ExitPlayMode();
    }

    /// <summary>每条连线由代码状态控制，且不能反复进入自身。</summary>
    [Test]
    public void GraphContainsAllStatesAndImmediateConditions()
    {
        var graph = controller.layers[0].stateMachine;
        Assert.AreEqual(12, graph.states.Length);
        Assert.AreEqual(14, graph.anyStateTransitions.Length);
        Assert.AreEqual("Locomotion", graph.defaultState.name);
        foreach (var transition in graph.anyStateTransitions)
        {
            Assert.IsFalse(transition.hasExitTime);
            Assert.IsFalse(transition.canTransitionToSelf);
            Assert.IsTrue(transition.hasFixedDuration);
            Assert.IsTrue(transition.orderedInterruption);
            Assert.IsTrue(transition.conditions.Any(condition => condition.parameter == "State"));
        }
        Assert.AreEqual("Dead", graph.anyStateTransitions[0].destinationState.name);
    }

    /// <summary>不离开 Attack 逻辑状态也能依次播放四段，然后回到移动。</summary>
    [Test]
    public void AnimatorPlaysEveryComboStepAndReturnsToLocomotion()
    {
        animator.SetInteger("State", (int)PlayerStateType.Attack);
        for (var i = 1; i <= 4; i++)
        {
            animator.SetInteger("ComboStep", i);
            AssertAnimation("Attack" + i);
        }
        animator.SetInteger("State", (int)PlayerStateType.Run);
        AssertAnimation("Locomotion");
    }

    /// <summary>技能、死亡、复活，以及同一个技能再次施放均能切换。</summary>
    [Test]
    public void AnimatorSupportsSkillDeathAndReplay()
    {
        animator.SetInteger("State", (int)PlayerStateType.Skill);
        animator.SetInteger("SkillIndex", 2);
        AssertAnimation("Skill2");
        animator.SetInteger("State", (int)PlayerStateType.Dead);
        AssertAnimation("Dead");
        animator.SetInteger("State", (int)PlayerStateType.Idle);
        AssertAnimation("Locomotion");
        animator.SetInteger("State", (int)PlayerStateType.Skill);
        AssertAnimation("Skill2");
    }

    /// <summary>推进过渡并确认 Animator 实际落在目标状态。</summary>
    private void AssertAnimation(string name)
    {
        for (var i = 0; i < 10; i++) animator.Update(0.02f);
        var state = animator.GetCurrentAnimatorStateInfo(0);
        Assert.IsTrue(state.IsName("Base Layer." + name), name + " actualHash=" + state.fullPathHash
            + " transitioning=" + animator.IsInTransition(0));
    }
}
