using NUnit.Framework;
using UnityEngine;

/// <summary>使用真实 CharacterController 验证位移和碰撞。</summary>
public sealed class PlayerMotorTests
{
    private GameObject ground;
    private GameObject actor;
    private GameObject cameraObject;
    private PlayerMotor motor;
    private PlayerSettings settings;
    private const float Step = 1f / 60f;

    /// <summary>在远离业务场景的位置建立地面、胶囊和相机。</summary>
    [SetUp]
    public void SetUp()
    {
        ground = new GameObject("PlayerMotorTestGround");
        ground.transform.position = new Vector3(1000f, -0.5f, 1000f);
        ground.AddComponent<BoxCollider>().size = new Vector3(100f, 1f, 100f);
        actor = new GameObject("PlayerMotorTestActor");
        actor.transform.position = new Vector3(1000f, 0.05f, 1000f);
        var controller = actor.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.center = Vector3.up;
        controller.radius = 0.3f;
        controller.minMoveDistance = 0f;
        cameraObject = new GameObject("PlayerMotorTestCamera");
        settings = new PlayerSettings();
        motor = new PlayerMotor(controller, cameraObject.transform, settings);
        Physics.SyncTransforms();
        for (var i = 0; i < 30; i++) motor.Tick(Vector2.zero, 0f, false, false, Step);
        Assert.IsTrue(motor.IsGrounded, "角色应先稳定落地。");
    }

    /// <summary>释放测试物体。</summary>
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(actor);
        Object.DestroyImmediate(ground);
        Object.DestroyImmediate(cameraObject);
    }

    /// <summary>相机右转后，向前输入按相机水平朝向移动。</summary>
    [Test]
    public void MoveUsesCameraYawAndRunSpeed()
    {
        cameraObject.transform.rotation = Quaternion.Euler(35f, 90f, 0f);
        var start = actor.transform.position;
        for (var i = 0; i < 60; i++) motor.Tick(Vector2.up, settings.RunSpeed, false, false, Step);
        Assert.Greater(actor.transform.position.x - start.x, 5.5f);
        Assert.AreEqual(start.z, actor.transform.position.z, 0.02f);
        Assert.AreEqual(settings.RunSpeed, motor.HorizontalSpeed, 0.02f);
    }

    /// <summary>斜向不超速，半幅摇杆保留半速。</summary>
    [TestCase(1f, 1f, 4f)]
    [TestCase(0f, 0.5f, 2f)]
    public void MovementPreservesInputMagnitude(float x, float y, float expectedSpeed)
    {
        for (var i = 0; i < 30; i++) motor.Tick(new Vector2(x, y), settings.WalkSpeed, false, false, Step);
        Assert.AreEqual(expectedSpeed, motor.HorizontalSpeed, 0.02f);
    }

    /// <summary>跳跃达到配置高度并回到地面。</summary>
    [Test]
    public void JumpReachesHeightAndLands()
    {
        var baseY = actor.transform.position.y;
        motor.Tick(Vector2.zero, 0f, true, false, Step);
        Assert.IsFalse(motor.IsGrounded);
        var peak = actor.transform.position.y;
        for (var i = 0; i < 120; i++)
        {
            motor.Tick(Vector2.zero, 0f, false, false, Step);
            peak = Mathf.Max(peak, actor.transform.position.y);
        }
        Assert.AreEqual(settings.JumpHeight, peak - baseY, 0.12f);
        Assert.IsTrue(motor.IsGrounded);
        Assert.AreEqual(baseY, actor.transform.position.y, 0.1f);
    }

    /// <summary>撞顶取消上升速度，随后下落。</summary>
    [Test]
    public void CeilingStopsAscent()
    {
        var ceiling = new GameObject("PlayerMotorTestCeiling");
        try
        {
            ceiling.transform.position = new Vector3(1000f, 2.8f, 1000f);
            ceiling.AddComponent<BoxCollider>().size = new Vector3(10f, 0.5f, 10f);
            Physics.SyncTransforms();
            motor.Tick(Vector2.zero, 0f, true, false, Step);
            for (var i = 0; i < 15 && motor.VerticalVelocity > 0f; i++)
                motor.Tick(Vector2.zero, 0f, false, false, Step);
            Assert.LessOrEqual(motor.VerticalVelocity, 0f);
            Assert.Less(actor.transform.position.y, 0.7f);
        }
        finally
        {
            Object.DestroyImmediate(ceiling);
        }
    }

    /// <summary>攻击锁定立即停止水平惯性，但重力仍保持角色接地。</summary>
    [Test]
    public void CombatLockStopsHorizontalMovement()
    {
        for (var i = 0; i < 20; i++) motor.Tick(Vector2.up, settings.RunSpeed, false, false, Step);
        var start = actor.transform.position;
        motor.Tick(Vector2.up, settings.RunSpeed, false, true, Step);
        Assert.AreEqual(start.z, actor.transform.position.z, 0.001f);
        Assert.AreEqual(0f, motor.HorizontalSpeed, 0.001f);
        Assert.IsTrue(motor.IsGrounded);
    }
}
