using UnityEngine;

/// <summary>本地角色入口，按输入、攻击、移动和表现的顺序组织一帧。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController), typeof(PlayerInputSource))]
[DefaultExecutionOrder(100)]
public sealed class PlayerController : MonoBehaviour
{
    /// <summary>本地输入采集组件；控制器读取输入帧，不直接绑定键盘按键。</summary>
    [SerializeField] private PlayerInputSource inputSource;
    /// <summary>角色视觉模型的 Animator；根节点位移由 CharacterController 控制。</summary>
    [SerializeField] private Animator animator;
    /// <summary>实际渲染相机的朝向，用于把前后左右输入换算为世界方向。</summary>
    [Tooltip("必填：实际渲染的主相机 Transform，用于把输入转换到世界方向。")]
    [SerializeField] private Transform cameraRoot;

    /// <summary>步行目标水平速度，单位米/秒。</summary>
    [Header("移动")]
    [SerializeField, Min(0.1f)] private float walkSpeed = 4f;
    /// <summary>奔跑目标水平速度，单位米/秒。</summary>
    [SerializeField, Min(0.1f)] private float runSpeed = 6.5f;
    /// <summary>水平加速和减速的变化率，单位米/秒平方；越大，启停越快。</summary>
    [SerializeField, Min(1f)] private float acceleration = 35f;
    /// <summary>每秒最多转向多少度；越大越快朝向输入方向。</summary>
    [SerializeField, Min(1f)] private float rotationSpeed = 720f;
    /// <summary>理论跳跃高度，单位米，结合重力计算起跳速度。</summary>
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.5f;
    /// <summary>竖直加速度，单位米/秒平方；负值表示向下。</summary>
    [SerializeField] private float gravity = -20f;
    /// <summary>最大下落速度的绝对值，单位米/秒，防止持续下落无限加速。</summary>
    [SerializeField, Min(1f)] private float terminalSpeed = 40f;

    /// <summary>共享的普攻配置资产；每个角色另建独立连击运行状态。</summary>
    [Header("普攻")]
    [SerializeField] private AttackComboDefinition attackCombo;

    /// <summary>当前移动状态，用于动画和网络同步；攻击由独立连击核心管理。</summary>
    public PlayerFsmCore.StateType State => fsm.State;
    /// <summary>碰撞处理后实际产生的水平速度，单位米/秒；顶墙时不会仍报告目标跑速。</summary>
    public float HorizontalSpeed { get; private set; }
    /// <summary>对外提供视觉模型入口，当前远端角色用它作为外观模板。</summary>
    public Animator VisualAnimator => animator;
    /// <summary>是否仍处于普攻动作的控制限制阶段。</summary>
    public bool IsAttacking => attacks.IsAttacking;
    /// <summary>当前或刚完成的连击段号，从 1 开始；无连击记录时为 0。</summary>
    public int ComboStep => attacks.CurrentStep;
    /// <summary>移动状态 Animator 参数的缓存哈希。</summary>
    private static readonly int StateParameter = Animator.StringToHash("State");
    /// <summary>实际水平速度 Animator 参数的缓存哈希。</summary>
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    /// <summary>业务层是否允许本地控制；进入世界未就绪、断线时会关闭。</summary>
    private bool controlEnabled = true;

    /// <summary>负责角色胶囊移动和碰撞的 Unity 组件。</summary>
    private CharacterController characterController;
    /// <summary>当前角色独立的移动状态机，决定走跑跳落的状态和目标速度。</summary>
    private PlayerFsmCore fsm;
    /// <summary>将连击核心的出招结果写到 Combat 动画层。</summary>
    private PlayerAttackController attacks;
    /// <summary>平滑加减速后的水平速度向量，单位米/秒，不含重力。</summary>
    private Vector3 horizontalVelocity;
    /// <summary>当前竖直速度，单位米/秒；正值上升，负值下落。</summary>
    private float verticalVelocity;
    /// <summary>胶囊原始台阶高度，单位米；离地时临时设为 0，落地后恢复。</summary>
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

        // input：本帧输入快照，攻击期间可暂时屏蔽其中的移动和跳跃。
        var input = inputSource.Sample();
        // 地面起跳优先于开始普攻；已出招期间不接受跳跃，离地则取消连击。
        // canAttack：当前是否接地且允许普攻；地面起跳优先于开始普攻。
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
        // direction：将平面输入按相机朝向换算后的世界移动方向。
        var direction = ToWorldDirection(input);
        // targetVelocity：当前输入方向乘以目标速度得到的水平速度向量，单位米/秒。
        var targetVelocity = direction * speed;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * deltaTime);

        Rotate(direction, deltaTime);

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        if (jump && characterController.isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity = Mathf.Max(verticalVelocity + gravity * deltaTime, -terminalSpeed);
        characterController.stepOffset = characterController.isGrounded ? groundedStepOffset : 0f;

        // beforeMove：本次胶囊移动前的位置，用于计算碰撞后的真实位移。
        var beforeMove = transform.position;
        // displacement：水平和竖直合成的速度，单位米/秒，乘帧时长后才是本帧位移。
        var displacement = horizontalVelocity + Vector3.up * verticalVelocity;
        // flags：胶囊本次移动的碰撞方向标记，例如头顶碰撞。
        var flags = characterController.Move(displacement * deltaTime);

        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;

        // actualMove：碰撞处理后实际产生的位移，单位米；去掉 Y 后计算水平速度。
        var actualMove = transform.position - beforeMove;
        actualMove.y = 0f;
        HorizontalSpeed = actualMove.magnitude / deltaTime;
    }

    /// <summary>按相机水平旋转，把 WASD 方向转换为世界方向。</summary>
    private Vector3 ToWorldDirection(Vector2 input)
    {
        // localDirection：把输入 X/Y 转成角色平面 X/Z 后的方向，尚未乘相机旋转。
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
        // wasEnabled：重新定位前胶囊是否启用，设置完位置后恢复原状态。
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
