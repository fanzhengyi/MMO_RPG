using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>本地和远端共用的动作特效播放方，只接收动作编号和进度。</summary>
public sealed class PlayerActionEffects
{
    private readonly Transform actor; // 胶囊角色的根节点，不是模型骨架根节点。
    private readonly PlayerCombatDefinition definition; // 本地与远端共用的动作资源配置。
    private readonly Transform leftBlade; // 仅绑定实际装备的左手武器。
    private readonly Transform rightBlade; // 实际装备的右手武器，构造时解析一次。
    private readonly List<PlayerActionEffectInstance> owned = new List<PlayerActionEffectInstance>(); // 包括旧动作尚未结束的世界特效。
    private readonly List<PlayerActionEffectInstance> current = new List<PlayerActionEffectInstance>(); // 当前动作生成的特效实例。
    private PlayerCombatDefinition.EffectCue[] cues = Array.Empty<PlayerCombatDefinition.EffectCue>(); // 当前动作尚需处理的触发标记。
    private bool[] triggered = Array.Empty<bool>(); // 每个动作实例只触发一次对应的特效标记。
    private float fullDuration; // 共享动作规则规定的完整时长，单位秒。
    private float previousElapsed; // 最近一次已处理的动作进度，单位秒。
    private int actionId; // 当前动作的稳定编号，仅用于查找资源和命名实例。
    private Vector3 castOrigin; // 出招时确认的位置，世界特效延迟触发时仍使用它。
    private Quaternion castRotation; // 出招时确认的水平朝向，不跟随后续角色转身。

    /// <summary>仍由本播放方拥有的特效数量，包含自然结束中的旧世界特效。</summary>
    public int ActiveCount => owned.Count;

    public PlayerActionEffects(Transform actor, Transform visual, PlayerCombatDefinition definition,
        Transform leftBlade = null, Transform rightBlade = null)
    {
        this.actor = actor;
        this.definition = definition;
        var transforms = visual.GetComponentsInChildren<Transform>(true);
        var leftHand = FindNamed(transforms, "hand_L");
        var rightHand = FindNamed(transforms, "hand_R");
        this.leftBlade = leftBlade != null ? leftBlade : FindBlade(transforms, leftHand);
        this.rightBlade = rightBlade != null ? rightBlade : FindBlade(transforms, rightHand);
    }

    /// <summary>切换动作；保留旧世界爆发的尾迹，补播新动作中仍然存活的特效。</summary>
    public void Start(int actionId, float elapsedSeconds, float fullDuration)
        => Start(actionId, elapsedSeconds, fullDuration, actor.position, actor.eulerAngles.y);

    /// <summary>按已确认的出招位置和朝向播放，迟到补播也不会将世界特效移到角色当前脚下。</summary>
    public void Start(int actionId, float elapsedSeconds, float fullDuration, Vector3 castOrigin, float castRotationY)
    {
        Finish();
        this.actionId = actionId;
        this.fullDuration = fullDuration;
        this.castOrigin = castOrigin;
        castRotation = Quaternion.Euler(0f, castRotationY, 0f);
        previousElapsed = elapsedSeconds;
        cues = definition.GetAction(actionId).Effects;
        triggered = new bool[cues.Length];
        for (var i = 0; i < cues.Length; i++)
        {
            var cueTime = cues[i].NormalizedTime * fullDuration;
            if (cueTime > elapsedSeconds) continue;
            triggered[i] = true;
            Spawn(cues[i], elapsedSeconds - cueTime);
        }
    }

    /// <summary>只处理本帧跨过的标记；相同或回退的进度不会重复生成特效。</summary>
    public void Tick(float elapsedSeconds)
    {
        if (elapsedSeconds <= previousElapsed) return;
        for (var i = 0; i < cues.Length; i++)
        {
            var cueTime = cues[i].NormalizedTime * fullDuration;
            if (triggered[i] || cueTime <= previousElapsed || cueTime > elapsedSeconds) continue;
            triggered[i] = true;
            Spawn(cues[i], elapsedSeconds - cueTime);
        }
        previousElapsed = elapsedSeconds;
    }

    /// <summary>正常收招：不再触发新标记，挂点特效停止跟随和发射，已有粒子自然结束。</summary>
    public void Finish()
    {
        foreach (var instance in current) instance.Finish();
        current.Clear();
        cues = Array.Empty<PlayerCombatDefinition.EffectCue>();
        triggered = Array.Empty<bool>();
    }

    /// <summary>中断、回正或销毁：立即移除这个角色拥有的全部特效。</summary>
    public void Stop()
    {
        foreach (var instance in owned) instance.Cancel();
        owned.Clear();
        current.Clear();
        cues = Array.Empty<PlayerCombatDefinition.EffectCue>();
        triggered = Array.Empty<bool>();
    }

    private void Spawn(PlayerCombatDefinition.EffectCue cue, float age)
    {
        if (age >= cue.Lifetime) return;
        var anchor = ResolveAnchor(cue.Anchor);
        // 左手没有装备时不伪造第二把刀；跟随中的挂点被销毁也属于正常生命周期。
        if (anchor == null) return;

        // 独立世界容器不在视觉模型子树中，复制角色外观时不会带出正在播放的特效。
        var container = new GameObject($"ActionFX_{actionId}_{cue.Anchor}");
        var isWorld = cue.Anchor == PlayerEffectAnchor.WorldForward;
        var position = isWorld ? castOrigin : anchor.position;
        var rotation = isWorld ? castRotation : anchor.rotation;
        container.transform.SetPositionAndRotation(position + rotation * cue.LocalPosition, rotation);
        var effect = UnityEngine.Object.Instantiate(cue.Prefab, container.transform, false);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = cue.Prefab.transform.localRotation * Quaternion.Euler(cue.LocalEuler);
        effect.transform.localScale = Vector3.Scale(cue.Prefab.transform.localScale, cue.LocalScale);

        var instance = container.AddComponent<PlayerActionEffectInstance>();
        instance.Initialize(this, effect, anchor, !isWorld,
            cue.LocalPosition, age, cue.Lifetime);
        owned.Add(instance);
        current.Add(instance);
    }

    private Transform ResolveAnchor(PlayerEffectAnchor anchor)
    {
        switch (anchor)
        {
            case PlayerEffectAnchor.BladeLeft: return leftBlade;
            case PlayerEffectAnchor.BladeRight: return rightBlade;
            default: return actor;
        }
    }

    private static Transform FindNamed(Transform[] transforms, string name)
    {
        foreach (var item in transforms)
            if (item.name == name) return item;
        return null;
    }

    private static Transform FindBlade(Transform[] transforms, Transform hand)
    {
        if (hand == null) return null;
        foreach (var item in transforms)
            if (item.name == "Sword_Saber" && item.IsChildOf(hand)) return item;
        return null;
    }

    internal void Remove(PlayerActionEffectInstance instance)
    {
        owned.Remove(instance);
        current.Remove(instance);
    }
}

/// <summary>单个特效的跟随和存活周期；无需角色继续推进动作计时。</summary>
internal sealed class PlayerActionEffectInstance : MonoBehaviour
{
    private PlayerActionEffects owner; // 销毁时通知所属播放方移除跟踪记录。
    private Transform anchor; // 角色或武器挂点，可能因离线或卸下武器而销毁。
    private Vector3 offset; // 挂点局部方向下的偏移，单位米。
    private ParticleSystem[] particles; // 完整特效中的所有粒子系统，生命周期由本容器接管。
    private double expiresAt; // 使用缩放后的 Unity 时钟计算的销毁时刻。
    private bool followsAnchor; // 正常收招后关闭跟随，世界爆发从不跟随。

    internal void Initialize(PlayerActionEffects owner, GameObject effect, Transform anchor,
        bool followsAnchor, Vector3 offset, float age, float lifetime)
    {
        this.owner = owner;
        this.anchor = anchor;
        this.followsAnchor = followsAnchor;
        this.offset = offset;
        expiresAt = Time.timeAsDouble + lifetime - age;
        particles = effect.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var particle in particles)
        {
            var main = particle.main;
            // 根粒子可能早于子粒子结束，由本容器统一负责销毁，避免截断子特效。
            main.stopAction = ParticleSystemStopAction.None;
        }
        if (age <= 0f) return;
        foreach (var particle in particles)
        {
            particle.Simulate(age, false, true, true);
            if (particle.IsAlive(false)) particle.Play(false);
        }
    }

    private void LateUpdate()
    {
        if (Time.timeAsDouble >= expiresAt || (followsAnchor && anchor == null))
        {
            Cancel();
            return;
        }
        if (followsAnchor)
            transform.SetPositionAndRotation(anchor.position + anchor.rotation * offset, anchor.rotation);
    }

    internal void Finish()
    {
        if (!followsAnchor) return;
        followsAnchor = false;
        foreach (var particle in particles)
            particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    internal void Cancel()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDestroy() => owner?.Remove(this);
}
