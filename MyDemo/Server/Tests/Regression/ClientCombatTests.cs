using System;

/// <summary>只推进客户端纯出招规则；实际按键边沿、动画和特效由 Unity 运行验证。</summary>
public static class ClientCombatTests
{
    public static void Run()
    {
        CheckAttackOrderAndSingleClock();
        CheckPausedInput();
        CheckSpecialPriorityAndCooldown();
        CheckBusyInputIsNotQueued();
        CheckCancelKeepsCooldownAndResetsCombo();
        CheckRecoveryAndTurnWindow();
        CheckRecoveryInputPriority();
    }

    private static void CheckAttackOrderAndSingleClock()
    {
        var core = new PlayerCombatCore();
        core.Tick(0.01f, true, 0, false, true);
        Require(core.ActionId == PlayerActionId.Attack01 && core.Generation == 1, "first attack starts once");
        core.Tick(0.125f, false, 0, false, true);
        Near(core.ElapsedSeconds, 0.125f, "one Tick advances action clock once");

        foreach (var expected in new[] { PlayerActionId.Attack02, PlayerActionId.Attack03, PlayerActionId.Attack01 })
        {
            var timing = PlayerActionRules.Get(core.ActionId);
            var generation = core.Generation;
            var untilBuffer = timing.Duration * timing.ChainAt - core.ElapsedSeconds - 0.125f;
            core.Tick(untilBuffer, true, 0, false, true);
            Require(core.Generation == generation, "buffering does not restart the current attack");
            core.Tick(0.126f, false, 0, false, true);
            Require(core.ActionId == expected && core.Generation == generation + 1, "buffered sequence cycles 1-2-3-1");
            Near(core.ElapsedSeconds, 0f, "next attack begins at zero rather than skipping a frame");
        }

        var lastGeneration = core.Generation;
        core.Tick(RecoveryOf(core.ActionId) + 0.001f, false, 0, false, true);
        Require(!core.IsBusy && core.Generation == lastGeneration, "normal recovery does not create a cancellation command");
        core.Tick(0.2f, false, 0, false, true);
        core.Tick(0.001f, true, 0, false, true);
        Require(core.ActionId == PlayerActionId.Attack02, "recovery frame cannot advance combo grace twice");
        core.Tick(RecoveryOf(core.ActionId) + 0.001f, false, 0, false, true);
        core.Tick(PlayerActionRules.ComboResetDelay + 0.001f, false, 0, false, true);
        core.Tick(0.001f, true, 0, false, true);
        Require(core.ActionId == PlayerActionId.Attack01, "expired continuation restarts first attack");
    }

    private static void CheckPausedInput()
    {
        var core = new PlayerCombatCore();
        core.Tick(0f, true, 4, true, true);
        Require(!core.IsBusy && core.Generation == 0, "paused input cannot start any action");
        core.Tick(0.01f, false, 1, false, true);
        var generation = core.Generation;
        var cooldown = core.CooldownRemaining(PlayerActionId.Skill01);
        core.Tick(0f, true, 2, true, true);
        Require(core.ActionId == PlayerActionId.Skill01 && core.Generation == generation, "paused input cannot interrupt or queue a busy action");
        Near(core.ElapsedSeconds, 0f, "pause does not progress the action");
        Near(core.CooldownRemaining(PlayerActionId.Skill01), cooldown, "pause does not progress cooldown");
        core.Cancel();
        core.Tick(0f, true, 0, false, true);
        Require(!core.IsBusy && core.ComboStep == 0, "paused click does not survive cancellation");
    }

    private static void CheckSpecialPriorityAndCooldown()
    {
        var slide = new PlayerCombatCore();
        slide.Tick(0.01f, true, 3, true, true);
        Require(slide.ActionId == PlayerActionId.Slide, "idle same-frame slide outranks skill and attack");
        Near(slide.CooldownRemaining(PlayerActionId.Skill03), 0f, "losing skill input does not spend its cooldown");
        Near(slide.CooldownRemaining(PlayerActionId.Slide), PlayerActionRules.Get(PlayerActionId.Slide).Cooldown,
            "slide cooldown begins on its start frame");
        slide.Tick(RecoveryOf(PlayerActionId.Slide) + 0.001f, false, 0, false, true);
        var generation = slide.Generation;
        slide.Tick(0.01f, false, 0, true, true);
        Require(!slide.IsBusy && slide.Generation == generation, "slide cannot restart before its cooldown");
        slide.Tick(slide.CooldownRemaining(PlayerActionId.Slide) + 0.001f, false, 0, false, true);
        slide.Tick(0.001f, false, 0, true, true);
        Require(slide.ActionId == PlayerActionId.Slide && slide.Generation == generation + 1,
            "new slide press after cooldown starts exactly once");

        for (var skill = 1; skill <= 4; skill++)
        {
            var core = new PlayerCombatCore();
            var id = (PlayerActionId)(10 + skill);
            core.Tick(0.01f, true, skill, false, true);
            Require(core.ActionId == id && core.ComboStep == 0, "skill outranks idle attack without carrying combo history");
            Near(core.CooldownRemaining(id), PlayerActionRules.Get(id).Cooldown, "skill spends its own cooldown");
            core.Tick(0.125f, false, 0, false, true);
            Near(core.CooldownRemaining(id), PlayerActionRules.Get(id).Cooldown - 0.125f, "cooldown clock advances once per frame");
            core.Tick(RecoveryOf(id) - core.ElapsedSeconds + 0.001f, false, 0, false, true);
            Require(!core.IsBusy, "skill releases control at its recovery point");
            generation = core.Generation;
            core.Tick(0.01f, false, skill, false, true);
            Require(!core.IsBusy && core.Generation == generation, "skill on cooldown cannot create a new generation");
            core.Tick(core.CooldownRemaining(id) + 0.001f, false, 0, false, true);
            core.Tick(0.001f, false, skill, false, true);
            Require(core.ActionId == id && core.Generation == generation + 1, "skill becomes available after its cooldown");
        }
    }

    private static void CheckBusyInputIsNotQueued()
    {
        var core = new PlayerCombatCore();
        core.Tick(0.01f, false, 1, false, true);
        var generation = core.Generation;
        core.Tick(0.125f, true, 2, true, true);
        Require(core.ActionId == PlayerActionId.Skill01 && core.Generation == generation,
            "busy skill ignores attack, other skills and slide");
        core.Tick(RecoveryOf(PlayerActionId.Skill01) - core.ElapsedSeconds + 0.001f, false, 0, false, true);
        core.Tick(0.5f, false, 0, false, true);
        Require(!core.IsBusy && core.Generation == generation, "ignored busy input never plays later");
        Near(core.CooldownRemaining(PlayerActionId.Skill02), 0f, "ignored skill does not consume cooldown");
        Near(core.CooldownRemaining(PlayerActionId.Slide), 0f, "ignored slide does not consume cooldown");
        core.Tick(0.01f, false, 2, false, true);
        Require(core.ActionId == PlayerActionId.Skill02, "fresh input after recovery works");
        core.Tick(RecoveryOf(PlayerActionId.Skill02) + 0.001f, true, 3, true, true);
        Require(core.ActionId == PlayerActionId.Slide, "recovery-frame inputs use normal slide-first priority");

        var attack = new PlayerCombatCore();
        attack.Tick(0.01f, true, 0, false, true);
        generation = attack.Generation;
        attack.Tick(0.125f, false, 2, true, true);
        Require(attack.ActionId == PlayerActionId.Attack01 && attack.Generation == generation,
            "busy attack cannot be cut short by skill or slide");
        attack.Tick(RecoveryOf(PlayerActionId.Attack01) - attack.ElapsedSeconds + 0.001f, false, 0, false, true);
        attack.Tick(0.125f, false, 0, false, true);
        Require(!attack.IsBusy && attack.Generation == generation, "busy attack did not queue a special action");
    }

    private static void CheckCancelKeepsCooldownAndResetsCombo()
    {
        var core = new PlayerCombatCore();
        core.Tick(0.01f, false, 2, false, true);
        var cooldown = core.CooldownRemaining(PlayerActionId.Skill02);
        core.Cancel();
        var cancelledGeneration = core.Generation;
        Require(!core.IsBusy && core.ComboStep == 0, "explicit cancellation clears action and combo");
        Near(core.CooldownRemaining(PlayerActionId.Skill02), cooldown, "cancellation never refunds a skill cooldown");
        core.Tick(0.25f, true, 1, true, false);
        Require(!core.IsBusy && core.Generation == cancelledGeneration, "airborne input cannot start or repeatedly cancel an idle action");
        Near(core.CooldownRemaining(PlayerActionId.Skill02), cooldown - 0.25f, "cooldown continues while airborne");

        core.Tick(0.01f, true, 0, false, true);
        var chain = PlayerActionRules.Get(PlayerActionId.Attack01);
        core.Tick(chain.Duration * chain.ChainAt - 0.125f, true, 0, false, true);
        var generation = core.Generation;
        core.Tick(0.01f, false, 0, false, false);
        Require(!core.IsBusy && core.ComboStep == 0 && core.Generation == generation + 1,
            "jump or loss of ground clears active attack and its buffered continuation once");
        core.Tick(0.5f, false, 0, false, true);
        Require(!core.IsBusy, "cancelled input buffer cannot trigger on landing");
        core.Tick(0.01f, true, 0, false, true);
        Require(core.ActionId == PlayerActionId.Attack01, "post-cancel attack starts from first stage");
        Require(core.CooldownRemaining(PlayerActionId.Skill02) > 0f, "combo cancellation does not erase unrelated skill cooldown");
    }

    private static void CheckRecoveryAndTurnWindow()
    {
        var core = new PlayerCombatCore();
        core.Tick(0.01f, true, 0, false, true);
        Require(core.CanTurn && core.IsAttacking, "attack startup permits direction adjustment");
        var timing = PlayerActionRules.Get(PlayerActionId.Attack01);
        core.Tick(timing.Duration * timing.TurnUntil + 0.001f, false, 0, false, true);
        Require(!core.CanTurn && core.IsAttacking, "attack direction locks after startup");
        core.Tick(timing.Duration * timing.RecoverAt - core.ElapsedSeconds + 0.001f, false, 0, false, true);
        Require(!core.IsBusy && !core.CanTurn, "recovery unlocks before full animation tail");
        Require(core.ElapsedSeconds < timing.Duration, "normal finish retains recovery progress rather than full clip duration");
        core.Tick(0.01f, false, 4, false, true);
        Require(core.ActionId == PlayerActionId.Skill04 && core.ComboStep == 0 && !core.CanTurn,
            "skill at attack recovery clears old continuation and locks direction");
    }

    private static void CheckRecoveryInputPriority()
    {
        var slide = new PlayerCombatCore();
        slide.Tick(0.01f, true, 0, false, true);
        slide.Tick(RecoveryOf(PlayerActionId.Attack01) + 0.001f, true, 3, true, true);
        Require(slide.ActionId == PlayerActionId.Slide,
            "at attack recovery same-frame slide still outranks a new skill and attack click");

        var skill = new PlayerCombatCore();
        skill.Tick(0.01f, true, 0, false, true);
        skill.Tick(RecoveryOf(PlayerActionId.Attack01) + 0.001f, true, 3, false, true);
        Require(skill.ActionId == PlayerActionId.Skill03,
            "at attack recovery same-frame skill still outranks a new attack click");
    }

    private static float RecoveryOf(PlayerActionId id)
    {
        var timing = PlayerActionRules.Get(id);
        return timing.Duration * timing.RecoverAt;
    }

    private static void Near(float actual, float expected, string message)
        => Require(Math.Abs(actual - expected) < 0.001f, $"{message}: expected {expected}, actual {actual}");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Client combat: " + message);
    }
}
