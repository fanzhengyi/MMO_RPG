using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>读取本地移动和攻击输入。</summary>
[DisallowMultipleComponent]
public sealed class PlayerInputSource : MonoBehaviour
{
    /// <summary>移动输入资产引用，一般对应 WASD 或左摇杆。</summary>
    [SerializeField] private InputActionReference moveAction;
    /// <summary>奔跑输入资产引用，按住生效。</summary>
    [SerializeField] private InputActionReference runAction;
    /// <summary>跳跃输入资产引用，只采集本帧按下。</summary>
    [SerializeField] private InputActionReference jumpAction;
    /// <summary>普攻输入资产引用，只采集本帧按下，不把长按视为持续点击。</summary>
    [SerializeField] private InputActionReference attackAction;

    /// <summary>本组件克隆的全部输入动作，启用和销毁时成组处理，不修改共享输入资产。</summary>
    private InputAction[] actions;
    /// <summary>本角色独立的移动动作副本。</summary>
    private InputAction move;
    /// <summary>本角色独立的奔跑动作副本。</summary>
    private InputAction run;
    /// <summary>本角色独立的跳跃动作副本。</summary>
    private InputAction jump;
    /// <summary>本角色独立的普攻动作副本。</summary>
    private InputAction attack;

    /// <summary>角色每帧读取的输入数据。</summary>
    public struct Frame
    {
        /// <summary>相机相对的平面输入；X 表示左右，Y 表示前后，向量长度限制为 0 到 1，避免斜向更快。</summary>
        public Vector2 Move;
        /// <summary>本帧是否按住奔跑键。</summary>
        public bool RunHeld;
        /// <summary>本帧是否刚按下跳跃键；长按的后续帧不会一直为 true。</summary>
        public bool JumpPressed;
        /// <summary>本帧是否刚按下普攻键；交给连击核心决定立即出招还是暂存输入。</summary>
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
        // action：当前遍历到的独立输入动作副本。
        foreach (var action in actions) action.Enable();
    }

    /// <summary>停用时关闭并释放输入副本，与 OnEnable 成对管理。</summary>
    private void OnDisable()
    {
        // action：当前遍历到的独立输入动作副本。
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
