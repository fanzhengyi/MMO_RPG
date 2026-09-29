using System;
using UnityEngine;

/// <summary>本地角色入口，按输入、状态、位移、表现的顺序组织一帧。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController), typeof(PlayerInputSource))]
public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputSource inputSource;
    [Tooltip("拖入实际渲染的主相机 Transform；为空时自动查找 MainCamera。")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private PlayerSettings settings = new PlayerSettings();

    public PlayerStateType State => fsm?.State ?? PlayerStateType.Idle;
    public int CurrentAttackStep => fsm?.CurrentAttackStep ?? 0;
    public int CurrentSkillIndex => fsm?.CurrentSkillIndex ?? 0;
    public float HorizontalSpeed => motor?.HorizontalSpeed ?? 0f;
    public float VerticalVelocity => motor?.VerticalVelocity ?? 0f;
    public bool IsGrounded => motor != null && motor.IsGrounded;
    public int LastInputSeq => inputSeq;
    public PlayerSettings Settings => settings;

    public event Action<PlayerStateType, PlayerStateType> StateChanged;
    public event Action JumpTriggered;
    public event Action<PlayerStateType, int> ActionStarted;
    public event Action<PlayerStateType, int> ActionImpact;
    // 同步适配层可订阅每个输入帧，自行批量发送，避免低频采样漏掉点击。
    public event Action<PlayerInputFrame> InputSampled;

    private CharacterController characterController;
    private PlayerMotor motor;
    private PlayerFsmCore fsm;
    private int inputSeq;

    /// <summary>组装输入、位移和状态机，并绑定业务事件。</summary>
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (inputSource == null)
            inputSource = GetComponent<PlayerInputSource>();
        if (cameraRoot == null && Camera.main != null)
            cameraRoot = Camera.main.transform;

        settings ??= new PlayerSettings();
        settings.Validate();
        motor = new PlayerMotor(characterController, cameraRoot, settings);
        fsm = new PlayerFsmCore(settings);
        fsm.StateChanged += OnStateChanged;
        fsm.ActionStarted += OnActionStarted;
        fsm.ActionImpact += OnActionImpact;
    }

    /// <summary>采样输入、推进状态，再执行一次 CharacterController 位移。</summary>
    private void Update()
    {
        if (Time.deltaTime <= 0f || !characterController.enabled)
            return;

        var input = inputSource.Sample(++inputSeq, transform.eulerAngles.y);
        fsm.Tick(input, Time.deltaTime, motor.IsGrounded, motor.VerticalVelocity);
        motor.Tick(input.Move, fsm.MovementSpeed, fsm.JumpRequested, fsm.BlocksMovement, Time.deltaTime);

        if (fsm.JumpRequested)
            JumpTriggered?.Invoke();
        InputSampled?.Invoke(input);
    }

    /// <summary>停用角色时清理速度，重新启用后不会继承旧惯性。</summary>
    private void OnDisable()
    {
        motor?.ResetMotion();
    }

    /// <summary>销毁时解除状态机事件。</summary>
    private void OnDestroy()
    {
        if (fsm == null)
            return;
        fsm.StateChanged -= OnStateChanged;
        fsm.ActionStarted -= OnActionStarted;
        fsm.ActionImpact -= OnActionImpact;
    }

    /// <summary>Inspector 修改配置时修正参数范围。</summary>
    private void OnValidate()
    {
        settings ??= new PlayerSettings();
        settings.Validate();
    }

    /// <summary>由生命值或服务器适配层调用，立即打断当前动作。</summary>
    public void SetDead()
    {
        fsm.SetDead();
    }

    /// <summary>复活到指定位置，并重置状态与技能冷却。</summary>
    public void Revive(Vector3 position, float rotationY)
    {
        motor.Teleport(position, rotationY);
        fsm.Reset();
    }

    /// <summary>外部传送入口；网络校正策略应放在独立同步适配层。</summary>
    public void Teleport(Vector3 position, float rotationY)
    {
        motor.Teleport(position, rotationY);
    }

    /// <summary>获取指定技能的剩余冷却，技能编号为 1-4。</summary>
    public float GetSkillCooldown(int index)
    {
        return fsm.GetSkillCooldown(index);
    }

    /// <summary>转发状态变化，供外围系统订阅。</summary>
    private void OnStateChanged(PlayerStateType previous, PlayerStateType next)
    {
        StateChanged?.Invoke(previous, next);
    }

    /// <summary>转发动作开始；index 为普攻段数或技能编号。</summary>
    private void OnActionStarted(PlayerStateType type, int index)
    {
        ActionStarted?.Invoke(type, index);
    }

    /// <summary>转发表现触发点，供音效和特效系统订阅。</summary>
    private void OnActionImpact(PlayerStateType type, int index)
    {
        ActionImpact?.Invoke(type, index);
    }
}
