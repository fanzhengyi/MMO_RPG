using UnityEngine;

/// <summary>本地角色入口，按输入、攻击、移动和表现的顺序组织一帧。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController), typeof(PlayerInputSource))]
[DefaultExecutionOrder(100)]
public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputSource inputSource;
    [SerializeField] private Animator animator;
    [Tooltip("必填：实际渲染的主相机 Transform，用于把输入转换到世界方向。")]
    [SerializeField] private Transform cameraRoot;

    [Header("移动")]
    [SerializeField, Min(0.1f)] private float walkSpeed = 4f;
    [SerializeField, Min(0.1f)] private float runSpeed = 6.5f;
    [SerializeField, Min(1f)] private float acceleration = 35f;
    [SerializeField, Min(1f)] private float rotationSpeed = 720f;
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField, Min(1f)] private float terminalSpeed = 40f;

    [Header("普攻")]
    [SerializeField] private AttackComboDefinition attackCombo;

    public PlayerFsmCore.StateType State => fsm.State;
    public float HorizontalSpeed { get; private set; }
    public Animator VisualAnimator => animator;
    public bool IsAttacking => attacks.IsAttacking;
    public int ComboStep => attacks.CurrentStep;
    private static readonly int StateParameter = Animator.StringToHash("State");
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private bool controlEnabled = true;

    private CharacterController characterController;
    private PlayerFsmCore fsm;
    private PlayerAttackController attacks;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float groundedStepOffset;

    /// <summary>记录胶囊原始台阶高度，避免离地时临时设为零后丢失配置。</summary>
    private void Awake()
        => groundedStepOffset = GetComponent<CharacterController>().stepOffset;

    /// <summary>每次启用都重建运行状态，也适用于编辑器脚本热重载。</summary>
    private void OnEnable()
    {
        characterController = GetComponent<CharacterController>();
        animator.applyRootMotion = false;
        fsm = new PlayerFsmCore(walkSpeed, runSpeed);
        attacks = new PlayerAttackController(animator, attackCombo);
        ResetMovement();
    }

    /// <summary>读取输入、推进状态机，再执行一次胶囊移动。</summary>
    private void Update()
    {
        if (!controlEnabled || Time.deltaTime <= 0f)
            return;

        var input = inputSource.Sample();
        // 地面起跳优先于开始普攻；已出招期间不接受跳跃，离地则取消连击。
        var canAttack = characterController.isGrounded && (attacks.IsAttacking || !input.JumpPressed);
        attacks.Tick(input.AttackPressed, canAttack, Time.deltaTime);
        if (attacks.IsAttacking)
        {
            if (attacks.CanTurn) Rotate(ToWorldDirection(input.Move), Time.deltaTime);
            input.Move = Vector2.zero;
            input.RunHeld = false;
            input.JumpPressed = false;
        }
        fsm.Tick(input, characterController.isGrounded, verticalVelocity);
        Move(input.Move, fsm.MovementSpeed, fsm.JumpRequested, Time.deltaTime);
        UpdateAnimation();
    }

    /// <summary>把输入按相机水平朝向换算，再处理跳跃、重力和碰撞。</summary>
    private void Move(Vector2 input, float speed, bool jump, float deltaTime)
    {
        var direction = ToWorldDirection(input);
        var targetVelocity = direction * speed;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * deltaTime);

        Rotate(direction, deltaTime);

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        if (jump && characterController.isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity = Mathf.Max(verticalVelocity + gravity * deltaTime, -terminalSpeed);
        characterController.stepOffset = characterController.isGrounded ? groundedStepOffset : 0f;

        var beforeMove = transform.position;
        var displacement = horizontalVelocity + Vector3.up * verticalVelocity;
        var flags = characterController.Move(displacement * deltaTime);

        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;

        var actualMove = transform.position - beforeMove;
        actualMove.y = 0f;
        HorizontalSpeed = actualMove.magnitude / deltaTime;
    }

    /// <summary>按相机水平旋转，把 WASD 方向转换为世界方向。</summary>
    private Vector3 ToWorldDirection(Vector2 input)
    {
        var localDirection = new Vector3(input.x, 0f, input.y);
        return Quaternion.Euler(0f, cameraRoot.eulerAngles.y, 0f) * localDirection;
    }

    private void Rotate(Vector3 direction, float deltaTime)
    {
        if (direction.sqrMagnitude <= 0.001f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * deltaTime);
    }

    /// <summary>停用角色时清除残留速度。</summary>
    private void OnDisable()
        => ResetMovement();

    /// <summary>业务层控制是否接受输入，控制器自身不依赖网络或账号数据。</summary>
    public void SetControlEnabled(bool value)
    {
        if (controlEnabled == value) return;
        controlEnabled = value;
        if (!value) ResetMovement();
    }

    /// <summary>出生或传送时暂时关闭胶囊，设置位置并清除旧速度。</summary>
    public void Place(Vector3 position, float rotationY)
    {
        var wasEnabled = characterController.enabled;
        characterController.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotationY, 0f));
        characterController.enabled = wasEnabled;
        ResetMovement();
    }

    /// <summary>停用、暂停控制和传送共用同一份速度与状态清理。</summary>
    private void ResetMovement()
    {
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        HorizontalSpeed = 0f;
        characterController.stepOffset = groundedStepOffset;
        attacks.Reset();
        fsm.ChangeState(fsm.Idle);
        UpdateAnimation();
    }

    /// <summary>把 FSM 状态和实际水平速度写入 Animator。</summary>
    private void UpdateAnimation()
    {
        animator.SetInteger(StateParameter, (int)fsm.State);
        animator.SetFloat(SpeedParameter, HorizontalSpeed);
    }
}
