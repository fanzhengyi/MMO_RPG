using UnityEngine;

/// <summary>负责相机方向换算、转向、跳跃和碰撞，不决定角色状态。</summary>
public sealed class PlayerMotor
{
    public bool IsGrounded { get; private set; }
    public float VerticalVelocity { get; private set; }
    public float HorizontalSpeed { get; private set; }

    private readonly CharacterController controller;
    private readonly Transform owner;
    private readonly Transform cameraRoot;
    private readonly PlayerSettings settings;
    private readonly float groundedStepOffset;
    private Vector3 horizontalVelocity;

    /// <summary>保存角色、相机和移动参数。</summary>
    public PlayerMotor(CharacterController controller, Transform cameraRoot, PlayerSettings settings)
    {
        this.controller = controller;
        owner = controller.transform;
        this.cameraRoot = cameraRoot;
        this.settings = settings;
        groundedStepOffset = controller.stepOffset;
        IsGrounded = controller.isGrounded;
    }

    /// <summary>执行水平移动和重力；即使攻击或死亡也会正常下落。</summary>
    public void Tick(Vector2 input, float speed, bool jump, bool stopImmediately, float deltaTime)
    {
        if (deltaTime <= 0f || !controller.enabled)
            return;

        var direction = ToWorldDirection(input);
        var targetVelocity = direction * speed;
        horizontalVelocity = stopImmediately ? Vector3.zero
            : Vector3.MoveTowards(horizontalVelocity, targetVelocity, settings.Acceleration * deltaTime);

        if (!stopImmediately && targetVelocity.sqrMagnitude > 0.001f)
            owner.rotation = Quaternion.RotateTowards(owner.rotation,
                Quaternion.LookRotation(direction), settings.RotationSpeed * deltaTime);

        if (IsGrounded && VerticalVelocity < 0f)
            VerticalVelocity = -2f;
        if (jump && IsGrounded)
        {
            VerticalVelocity = Mathf.Sqrt(-2f * settings.Gravity * settings.JumpHeight);
            IsGrounded = false;
        }

        controller.stepOffset = IsGrounded ? groundedStepOffset : 0f;
        var nextVertical = Mathf.Max(VerticalVelocity + settings.Gravity * deltaTime, -settings.TerminalSpeed);
        var displacement = horizontalVelocity * deltaTime;
        displacement.y = (VerticalVelocity + nextVertical) * 0.5f * deltaTime;
        VerticalVelocity = nextVertical;

        var previousPosition = owner.position;
        var flags = controller.Move(displacement);
        IsGrounded = (flags & CollisionFlags.Below) != 0 && VerticalVelocity <= 0f;
        if ((flags & CollisionFlags.Above) != 0 && VerticalVelocity > 0f)
            VerticalVelocity = 0f;
        if (IsGrounded)
            VerticalVelocity = -2f;

        var actualMove = owner.position - previousPosition;
        actualMove.y = 0f;
        HorizontalSpeed = actualMove.magnitude / deltaTime;
    }

    /// <summary>按相机水平朝向移动，保留摇杆幅度并限制斜向速度。</summary>
    private Vector3 ToWorldDirection(Vector2 input)
    {
        input = Vector2.ClampMagnitude(input, 1f);
        var local = new Vector3(input.x, 0f, input.y);
        return cameraRoot == null ? local : Quaternion.Euler(0f, cameraRoot.eulerAngles.y, 0f) * local;
    }

    /// <summary>清除速度和旧接地结果，恢复地面跨台阶高度。</summary>
    public void ResetMotion()
    {
        horizontalVelocity = Vector3.zero;
        VerticalVelocity = 0f;
        HorizontalSpeed = 0f;
        IsGrounded = false;
        controller.stepOffset = groundedStepOffset;
    }

    /// <summary>临时关闭胶囊碰撞后传送，并清除传送前的速度。</summary>
    public void Teleport(Vector3 position, float rotationY)
    {
        var wasEnabled = controller.enabled;
        controller.enabled = false;
        owner.SetPositionAndRotation(position, Quaternion.Euler(0f, rotationY, 0f));
        controller.enabled = wasEnabled;
        ResetMotion();
    }
}
