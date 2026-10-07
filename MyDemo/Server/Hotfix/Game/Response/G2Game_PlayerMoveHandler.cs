using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>校验 Gate 转发的移动快照，更新在线位置并广播给其他玩家。</summary>
public sealed class G2Game_PlayerMoveHandler : Address<Scene, G2Game_PlayerMove>
{
    // 位移速度上限使用游戏世界单位/秒；宽限距离吸收采样间隔和物理更新带来的小误差。
    private const float MaxHorizontalSpeed = 9f;
    private const float HorizontalGraceDistance = 1f;
    private const float MaxVerticalSpeed = 45f;
    private const float VerticalGraceDistance = 2f;
    // 限制客户端上报给动画使用的 Speed 参数范围。
    private const float MaxReportedSpeed = 8f;
    //位置自动保存间隔（毫秒）
    private const long PositionSaveIntervalMs = 5000;

    protected override async FTask Run(Scene scene, G2Game_PlayerMove request)
    {
        // 拒绝缺少路由身份、非有限坐标、异常速度和未知动画状态，避免坏数据进入共享角色状态。
        if (string.IsNullOrEmpty(request.UserName)
            || request.GateSessionRuntimeId == 0
            || request.GateSceneAddress == 0
            || !IsFinite(request.X) || !IsFinite(request.Y) || !IsFinite(request.Z)
            || !IsFinite(request.RotationY) || !IsFinite(request.Speed)
            || request.Speed < 0f || request.Speed > MaxReportedSpeed
            || !IsValidState(request.State))
        {
            return;
        }

        var onlineComponent = scene.GetComponent<OnlineComponent>();
        // 只有已经加载到 Game 在线表的角色才能移动。
        if (!onlineComponent.PlayersByName.TryGetValue(request.UserName, out var player))
        {
            return;
        }

        // 只有当前绑定的 Gate Session 才能更新这个角色，旧连接和未进游戏的连接会被丢弃。
        if (player.GateSessionRuntimeId != request.GateSessionRuntimeId
            || player.GateSceneAddress != request.GateSceneAddress)
        {
            return;
        }

        var now = Environment.TickCount64; //服务器进程启动后的毫秒数
        // 首条快照没有本连接的比较基准；后续快照必须序号更新，且位移符合经过时间。
        if (player.HasLastMoveSequence
            && (!IsNewerSequence(request.Sequence, player.LastMoveSequence)
                || !IsPlausibleMovement(player, request, now)))
        {
            return;
        }

        // 校验通过后才更新服务器持有的位置、朝向和动画状态。
        var stateChanged = player.MovementState != request.State;
        player.X = request.X;
        player.Y = request.Y;
        player.Z = request.Z;
        player.RotationY = NormalizeAngle(request.RotationY);
        player.MovementState = request.State;
        player.MovementSpeed = request.Speed;
        player.LastMoveSequence = request.Sequence;
        player.HasLastMoveSequence = true;
        player.LastMoveReceivedAt = now;

        // Game 不直接保存客户端连接；按每个接收者记录的 Gate 地址和 Session ID 转发。
        foreach (var receiver in onlineComponent.Players.Values)
        {
            // 不把自己的移动发回发送者，也不向已断开或尚未绑定 Gate 的角色转发。
            if (receiver.Id == player.Id
                || receiver.GateSessionRuntimeId == 0
                || receiver.GateSceneAddress == 0)
            {
                continue;
            }

            scene.Send(receiver.GateSceneAddress, new Game2G_PlayerMove
            {
                GateSessionRuntimeId = receiver.GateSessionRuntimeId,
                RoleId = player.Id,
                X = player.X,
                Y = player.Y,
                Z = player.Z,
                RotationY = player.RotationY,
                Sequence = player.LastMoveSequence,
                State = player.MovementState,
                Speed = player.MovementSpeed,
            });
        }

        // 移动时最多每隔一段时间写一次数据库；进入 Idle 时立即保存停下来的位置。
        if (now - player.LastPositionSavedAt >= PositionSaveIntervalMs
            || (stateChanged && request.State == 0))
        {
            player.LastPositionSavedAt = now;
            await scene.World.Database.Save(player);
        }
    }

    /// <summary>判断序号是否更新；用有符号差值处理 uint 从最大值回绕到 0 的情况。</summary>
    private static bool IsNewerSequence(uint sequence, uint previous)
    {
        return unchecked((int)(sequence - previous)) > 0;
    }

    /// <summary>按两条快照之间经过的时间，检查水平和垂直位移是否超过允许速度。</summary>
    private static bool IsPlausibleMovement(OnlineInfo player, G2Game_PlayerMove request, long now)
    {
        // 至少按 0.05 秒计算，避免两条紧挨着到达的消息产生近乎为零的速度预算。
        var elapsedSeconds = Math.Max((now - player.LastMoveReceivedAt) / 1000f, 0.05f);
        var deltaX = request.X - player.X;
        var deltaY = request.Y - player.Y;
        var deltaZ = request.Z - player.Z;
        var horizontalDistance = MathF.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        var verticalDistance = MathF.Abs(deltaY);

        // 水平方向只比较 XZ；Y 单独比较，允许跳跃和下落速度高于地面移动。
        return horizontalDistance <= MaxHorizontalSpeed * elapsedSeconds + HorizontalGraceDistance
            && verticalDistance <= MaxVerticalSpeed * elapsedSeconds + VerticalGraceDistance;
    }

    /// <summary>只接受客户端状态机定义的 Idle、Move、Jump、Run、Fall 状态。</summary>
    private static bool IsValidState(int state)
    {
        return state == 0 || state == 1 || state == 2 || state == 6 || state == 7;
    }

    /// <summary>拒绝 NaN 和正负无穷大，避免异常浮点数污染角色状态。</summary>
    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>把朝向统一到 0 到 360 度，避免累计或负角度造成表示不一致。</summary>
    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        return angle < 0f ? angle + 360f : angle;
    }
}
