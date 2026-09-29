using UnityEngine;

/// <summary>本地角色入口，按输入、状态、位移、表现的顺序组织一帧。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController), typeof(PlayerInputSource))]
[DefaultExecutionOrder(100)]
public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputSource inputSource;
    [SerializeField] private Animator animator;
    [Tooltip("拖入实际渲染的主相机 Transform；为空时自动查找 MainCamera。")]
    [SerializeField] private Transform cameraRoot;

    [Header("移动")]
    [SerializeField, Min(0.1f)] private float walkSpeed = 4f;
    [SerializeField, Min(0.1f)] private float runSpeed = 6.5f;
    [SerializeField, Min(1f)] private float acceleration = 35f;
    [SerializeField, Min(1f)] private float rotationSpeed = 720f;
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField, Min(1f)] private float terminalSpeed = 40f;
    [SerializeField, Min(0f)] private float animationDampTime = 0.08f;

    public PlayerFsmCore.StateType State => fsm.State;
    public float HorizontalSpeed { get; private set; }

    private CharacterController characterController;
    private PlayerFsmCore fsm;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float groundedStepOffset;

    /// <summary>获取组件、相机，并创建状态机。</summary>
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        groundedStepOffset = characterController.stepOffset;
        fsm = new PlayerFsmCore();
    }

    /// <summary>读取输入、推进状态机，再执行一次胶囊移动。</summary>
    private void Update()
    {
        if (Time.deltaTime <= 0f)
            return;

        var input = inputSource.Sample();
        fsm.Tick(input, characterController.isGrounded, verticalVelocity, walkSpeed, runSpeed);
        Move(input.Move, fsm.MovementSpeed, fsm.JumpRequested, Time.deltaTime);
        UpdateAnimation();
    }

    /// <summary>把输入按相机水平朝向换算，再处理跳跃、重力和碰撞。</summary>
    private void Move(Vector2 input, float speed, bool jump, float deltaTime)
    {
        var direction = ToWorldDirection(input);
        var targetVelocity = direction * speed;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * deltaTime);

        if (direction.sqrMagnitude > 0.001f)
        {
            var targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * deltaTime);
        }

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
        var view =cameraRoot;
        if (view == null)
            return localDirection;

        return Quaternion.Euler(0f, view.eulerAngles.y, 0f) * localDirection;
    }

    /// <summary>停用角色时清除残留速度。</summary>
    private void OnDisable()
    {
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        HorizontalSpeed = 0f;
        if (characterController != null)
            characterController.stepOffset = groundedStepOffset;
    }

    /// <summary>限制 Inspector 中的移动参数。</summary>
    private void OnValidate()
    {
        walkSpeed = Mathf.Max(0.1f, walkSpeed);
        runSpeed = Mathf.Max(walkSpeed, runSpeed);
        acceleration = Mathf.Max(1f, acceleration);
        rotationSpeed = Mathf.Max(1f, rotationSpeed);
        jumpHeight = Mathf.Max(0.1f, jumpHeight);
        gravity = Mathf.Min(-0.1f, gravity);
        terminalSpeed = Mathf.Max(1f, terminalSpeed);
    }

    /// <summary>把 FSM 状态和实际水平速度写入 Animator。</summary>
    private void UpdateAnimation()
    {
        animator.SetInteger("State", (int)fsm.State);
        animator.SetFloat("Speed", HorizontalSpeed, animationDampTime, Time.deltaTime);
    }
}
