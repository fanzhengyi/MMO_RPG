using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>读取移动、奔跑和跳跃输入。</summary>
[DisallowMultipleComponent]
public sealed class PlayerInputSource : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference runAction;
    [SerializeField] private InputActionReference jumpAction;

    private readonly List<InputAction> actions = new List<InputAction>(3);
    private InputAction move;
    private InputAction run;
    private InputAction jump;

    /// <summary>FSM 每帧读取的输入数据。</summary>
    public struct Frame
    {
        public Vector2 Move;
        public bool RunHeld;
        public bool JumpPressed;
    }

    /// <summary>复制配置的 Action，避免影响共享的 Input Actions。</summary>
    private void Awake()
    {
        move = Clone(moveAction);
        run = Clone(runAction);
        jump = Clone(jumpAction);
        if (actions.Count != 3)
            Debug.LogWarning("PlayerInputSource: 请配置 Move、Run 和 Jump。", this);
    }

    /// <summary>启用本角色持有的输入副本。</summary>
    private void OnEnable()
    {
        foreach (var action in actions) action.Enable();
    }

    /// <summary>停用本角色持有的输入副本。</summary>
    private void OnDisable()
    {
        foreach (var action in actions) action.Disable();
    }

    /// <summary>释放复制的输入 Action。</summary>
    private void OnDestroy()
    {
        foreach (var action in actions) action.Dispose();
    }

    /// <summary>采集当前移动方向、奔跑按住状态和跳跃单次按下。</summary>
    public Frame Sample()
    {
        if (!isActiveAndEnabled)
            return default;

        return new Frame
        {
            Move = move == null ? Vector2.zero : Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f),
            RunHeld = run != null && run.IsPressed(),
            JumpPressed = jump != null && jump.WasPressedThisFrame()
        };
    }

    /// <summary>复制一个输入引用并登记它的生命周期。</summary>
    private InputAction Clone(InputActionReference reference)
    {
        if (reference == null || reference.action == null)
            return null;

        var action = reference.action.Clone();
        actions.Add(action);
        return action;
    }
}
