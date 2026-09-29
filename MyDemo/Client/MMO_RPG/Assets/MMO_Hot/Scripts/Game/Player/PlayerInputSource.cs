using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>把 Input Actions 转成一帧输入；运行时独立持有 Action 副本。</summary>
[DisallowMultipleComponent]
public sealed class PlayerInputSource : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference runAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Combat")]
    [SerializeField] private InputActionReference attackAction;
    // 下标 0-3 分别对应 Skill1-Skill4。
    [SerializeField] private InputActionReference[] skillActions = new InputActionReference[PlayerSettings.SkillCount];
    [SerializeField] private bool blockAttackOverUI = true;

    private readonly List<InputAction> ownedActions = new List<InputAction>(8);
    private readonly InputAction[] skills = new InputAction[PlayerSettings.SkillCount];
    private InputAction move;
    private InputAction run;
    private InputAction jump;
    private InputAction attack;

    /// <summary>复制配置的 Action，避免一个角色停用输入时关闭其他对象的输入。</summary>
    private void Awake()
    {
        move = CloneAction(moveAction);
        run = CloneAction(runAction);
        jump = CloneAction(jumpAction);
        attack = CloneAction(attackAction);
        for (var i = 0; i < skills.Length; i++)
            skills[i] = CloneAction(skillActions != null && i < skillActions.Length ? skillActions[i] : null);

        if (ownedActions.Count != 8)
            Debug.LogWarning("PlayerInputSource: 请配置 Move、Run、Jump、Attack 和 Skill1-4。", this);
    }

    /// <summary>启用当前角色持有的输入副本。</summary>
    private void OnEnable()
    {
        foreach (var action in ownedActions)
            action.Enable();
    }

    /// <summary>停用当前角色的输入副本。</summary>
    private void OnDisable()
    {
        foreach (var action in ownedActions)
            action.Disable();
    }

    /// <summary>释放运行时创建的 Action。</summary>
    private void OnDestroy()
    {
        foreach (var action in ownedActions)
            action.Dispose();
    }

    /// <summary>读取本帧移动、按住状态和单次点击；不执行角色行为。</summary>
    public PlayerInputFrame Sample(int inputSeq, float rotationY)
    {
        var frame = new PlayerInputFrame
        {
            InputSeq = inputSeq,
            ClientTick = Mathf.RoundToInt(Time.time * 1000f),
            RotationY = rotationY
        };
        if (!isActiveAndEnabled)
            return frame;

        frame.Move = move == null ? Vector2.zero : Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        frame.IsRun = run != null && run.IsPressed();
        frame.JumpPressed = jump != null && jump.WasPressedThisFrame();
        frame.AttackPressed = attack != null && attack.WasPressedThisFrame()
            && !(blockAttackOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
        for (var i = 0; i < skills.Length; i++)
        {
            if (skills[i] == null || !skills[i].WasPressedThisFrame())
                continue;
            frame.SkillIndex = i + 1;
            break;
        }
        return frame;
    }

    /// <summary>固定技能引用数量并保留已有配置。</summary>
    private void OnValidate()
    {
        Array.Resize(ref skillActions, PlayerSettings.SkillCount);
    }

    /// <summary>复制单个 Action 并登记生命周期；缺少引用时返回 null。</summary>
    private InputAction CloneAction(InputActionReference reference)
    {
        if (reference == null || reference.action == null)
            return null;
        var action = reference.action.Clone();
        ownedActions.Add(action);
        return action;
    }
}
