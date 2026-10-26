using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>特效挂点；世界特效只记录出招位置，其他挂点在播放期间跟随。</summary>
public enum PlayerEffectAnchor
{
    Actor,
    BladeLeft,
    BladeRight,
    WorldForward
}

/// <summary>动作的动画和特效资源，不保存玩家状态，也不重复定义动作规则时长。</summary>
[CreateAssetMenu(menuName = "Game/Combat/Player Combat", fileName = "PlayerCombat")]
public sealed class PlayerCombatDefinition : ScriptableObject
{
    [Serializable]
    public sealed class EffectCue
    {
        /// <summary>包含完整粒子层级的特效资源。</summary>
        public GameObject Prefab;
        /// <summary>相对动作完整时长的触发进度，零表示出招时生成。</summary>
        [Range(0f, 1f)] public float NormalizedTime;
        /// <summary>跟随角色、实际武器，或固定在本次施法的世界位置。</summary>
        public PlayerEffectAnchor Anchor;
        /// <summary>挂点或施法朝向下的偏移，单位米。</summary>
        [Tooltip("挂点局部方向下的偏移，单位米，不叠加角色缩放。")]
        public Vector3 LocalPosition;
        /// <summary>在预制体作者轴向之后附加的欧拉角。</summary>
        [Tooltip("保留预制体原有轴向后附加的旋转，单位度。")]
        public Vector3 LocalEuler;
        /// <summary>相对预制体原始缩放的倍率，不继承角色缩放。</summary>
        public Vector3 LocalScale = Vector3.one;
        /// <summary>从标记触发起允许存活的总秒数，包含延迟和子粒子尾迹。</summary>
        [Tooltip("整个特效（包括子粒子）允许存活的秒数，由播放方统一销毁。")]
        [Min(0.01f)] public float Lifetime = 6f;
    }

    [Serializable]
    public sealed class ActionDefinition
    {
        /// <summary>共享动作规则使用的稳定编号，与 Animator 状态编号无关。</summary>
        [Tooltip("稳定动作编号：普攻 1..3、技能 11..14、滑步 20。")]
        public int ActionId;
        /// <summary>该动作的动画资源，实际播放速度由共享规则时长换算。</summary>
        public AnimationClip Clip;
        /// <summary>独立的特效触发标记，可按需配置多个挂点或不同触发时刻。</summary>
        public EffectCue[] Effects = Array.Empty<EffectCue>();
    }

    // 动作资源配置；同一资产可供本地和远端共享，不存储播放进度。
    [SerializeField] private ActionDefinition[] actions = Array.Empty<ActionDefinition>();

    public IReadOnlyList<ActionDefinition> Actions => actions;

    /// <summary>动作开始时查找一次；缺失配置直接暴露装配错误。</summary>
    public ActionDefinition GetAction(int actionId)
    {
        foreach (var action in actions)
            if (action.ActionId == actionId) return action;
        throw new KeyNotFoundException($"未配置动作 {actionId} 的动画和特效。");
    }
}
