using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>读取本地移动和攻击输入。</summary>
[DisallowMultipleComponent]
public sealed class PlayerInputSource : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference runAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference attackAction;

    private InputAction[] actions;
    private InputAction move;
    private InputAction run;
    private InputAction jump;
    private InputAction attack;

    /// <summary>角色每帧读取的输入数据。</summary>
    public struct Frame
    {
        public Vector2 Move;
        public bool RunHeld;
        public bool JumpPressed;
        public bool AttackPressed;
    }

    /// <summary>启用时复制并开启 Action；重新启用或热重载后使用一组完整的新副本。</summary>
    private void OnEnable()
    {
        move = moveAction.action.Clone();
        run = runAction.action.Clone();
        jump = jumpAction.action.Clone();
        attack = attackAction.action.Clone();
        actions = new[] { move, run, jump, attack };
        foreach (var action in actions) action.Enable();
    }

    /// <summary>停用时关闭并释放输入副本，与 OnEnable 成对管理。</summary>
    private void OnDisable()
    {
        foreach (var action in actions)
        {
            action.Disable();
            action.Dispose();
        }
    }

    /// <summary>采集移动方向、奔跑状态，以及跳跃和攻击的单次按下。</summary>
    public Frame Sample()
    {
        if (!isActiveAndEnabled)
            return default;

        return new Frame
        {
            Move = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f),
            RunHeld = run.IsPressed(),
            JumpPressed = jump.WasPressedThisFrame(),
            AttackPressed = attack.WasPressedThisFrame()
        };
    }
}
