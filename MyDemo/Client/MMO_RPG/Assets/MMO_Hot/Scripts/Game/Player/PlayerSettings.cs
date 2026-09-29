using System;
using UnityEngine;

/// <summary>角色参数；时间单位为秒，窗口和命中点使用动作进度 0-1。</summary>
[Serializable]
public sealed class PlayerSettings
{
    public const int AttackCount = 4;
    public const int SkillCount = 4;

    [Header("Movement")]
    [Min(0.1f)] public float WalkSpeed = 4f;
    [Min(0.1f)] public float RunSpeed = 6.5f;
    [Min(1f)] public float Acceleration = 35f;
    [Min(1f)] public float RotationSpeed = 720f;
    [Min(0.1f)] public float JumpHeight = 1.5f;
    public float Gravity = -20f;
    [Min(1f)] public float TerminalSpeed = 40f;

    [Header("Combat")]
    public PlayerAttackSettings[] Attacks = new PlayerAttackSettings[AttackCount];
    public PlayerSkillSettings[] Skills = new PlayerSkillSettings[SkillCount];

    /// <summary>补齐四段普攻和四个技能的默认配置。</summary>
    public PlayerSettings()
    {
        Validate();
    }

    /// <summary>修正无效参数，并保留已有的动作配置。</summary>
    public void Validate()
    {
        WalkSpeed = Mathf.Max(0.1f, WalkSpeed);
        RunSpeed = Mathf.Max(WalkSpeed, RunSpeed);
        Acceleration = Mathf.Max(1f, Acceleration);
        RotationSpeed = Mathf.Max(1f, RotationSpeed);
        JumpHeight = Mathf.Max(0.1f, JumpHeight);
        Gravity = Mathf.Min(-0.1f, Gravity);
        TerminalSpeed = Mathf.Max(1f, TerminalSpeed);
        Array.Resize(ref Attacks, AttackCount);
        Array.Resize(ref Skills, SkillCount);
        for (var i = 0; i < AttackCount; i++)
        {
            Attacks[i] ??= new PlayerAttackSettings();
            Attacks[i].Validate();
        }
        for (var i = 0; i < SkillCount; i++)
        {
            Skills[i] ??= new PlayerSkillSettings();
            Skills[i].Validate();
        }
    }
}

/// <summary>一次动作的持续时间和表现触发点。</summary>
[Serializable]
public class PlayerActionSettings
{
    [Tooltip("动作总时长，应与动画片段长度 / 播放速度一致。")]
    [Min(0.05f)] public float Duration = 0.8f;
    [Tooltip("触发表现事件的进度；伤害由战斗系统或服务器判定。")]
    [Range(0f, 1f)] public float ImpactTime = 0.4f;

    /// <summary>限制时长与触发点的合法范围。</summary>
    public virtual void Validate()
    {
        Duration = Mathf.Max(0.05f, Duration);
        ImpactTime = Mathf.Clamp01(ImpactTime);
    }
}

/// <summary>一段普攻的点击窗口与下一段起播时间。</summary>
[Serializable]
public sealed class PlayerAttackSettings : PlayerActionSettings
{
    [Range(0f, 1f)] public float ComboOpen = 0.25f;
    [Range(0f, 1f)] public float ComboClose = 0.7f;
    [Range(0f, 1f)] public float ComboTransition = 0.8f;

    /// <summary>保证窗口关闭后才接下一段，且不会跳过命中点。</summary>
    public override void Validate()
    {
        base.Validate();
        ComboOpen = Mathf.Clamp(ComboOpen, 0f, 0.95f);
        ComboClose = Mathf.Clamp(ComboClose, ComboOpen, 0.95f);
        ComboTransition = Mathf.Clamp(ComboTransition, Mathf.Max(ComboClose, ImpactTime), 1f);
    }
}

/// <summary>一个技能的动作时间与冷却时间。</summary>
[Serializable]
public sealed class PlayerSkillSettings : PlayerActionSettings
{
    [Tooltip("从技能开始施放时计时。")]
    [Min(0f)] public float Cooldown = 2f;

    /// <summary>修正技能时间和冷却时间。</summary>
    public override void Validate()
    {
        base.Validate();
        Cooldown = Mathf.Max(0f, Cooldown);
    }
}
