using Fantasy;
using Fantasy.MongdbModel;

/// <summary>统一管理世界可见性与复制；不校验输入，不访问数据库。</summary>
public static class WorldReplication
{
    /// <summary>世界移动广播周期，单位毫秒；50 毫秒表示每秒最多处理 20 轮，合并轮内多次采样。</summary>
    private const int BroadcastIntervalMs = 50;

    public static void Start(OnlineComponent online)
    {
        // runtimeId：开始操作时记录的对象运行时 ID，用于识别销毁或对象池复用。
        var runtimeId = online.RuntimeId;
        online.ReplicationTimerId = online.Scene.TimerComponent.Net.RepeatedTimer(BroadcastIntervalMs, () =>
        {
            if (!online.IsDisposed && online.RuntimeId == runtimeId) Flush(online);
        });
    }

    /// <summary>集中定义当前可见范围；目前是一个公共场景，扩展 AOI 时替换此处。</summary>
    public static IEnumerable<OnlineInfo> VisiblePlayers(Scene scene, OnlineInfo observer)
    {
        // player：当前处理的服务端角色数据，包含持久资料和本次在线状态。
        foreach (var player in scene.GetComponent<OnlineComponent>().PlayersByName.Values)
            if (player.Id != observer.Id && player.IsInWorld) yield return player;
    }

    public static void PublishEntered(Scene scene, OnlineInfo player)
    {
        // receiver：本条世界通知的接收玩家，用其 Gate 路由定向发送。
        foreach (var receiver in VisiblePlayers(scene, player))
            scene.Send(receiver.GateSceneAddress, new Game2G_PlayerEntered
            {
                GateSessionRuntimeId = receiver.GateSessionRuntimeId,
                WorldSessionId = receiver.WorldSessionId,
                Player = PlayerMessageMapper.ToPresence(player),
            });
    }

    public static void PublishLeft(Scene scene, OnlineInfo player)
    {
        if (!player.IsInWorld) return;
        // receiver：本条世界通知的接收玩家，用其 Gate 路由定向发送。
        foreach (var receiver in VisiblePlayers(scene, player))
            scene.Send(receiver.GateSceneAddress, new Game2G_PlayerLeft
            {
                GateSessionRuntimeId = receiver.GateSessionRuntimeId,
                WorldSessionId = receiver.WorldSessionId,
                RoleId = player.Id,
                SpawnId = player.WorldSessionId,
                SpawnRevision = player.SpawnRevision,
            });
    }

    /// <summary>同一复制周期只广播最后的确认状态，输入包频率不能放大世界广播。</summary>
    private static void Flush(OnlineComponent online)
    {
        // now：服务端单调时钟的当前毫秒值，不是现实日期。
        var now = Environment.TickCount64;
        // player：当前处理的服务端角色数据，包含持久资料和本次在线状态。
        foreach (var player in online.PlayersByName.Values)
        {
            if (!player.NeedsReplication) continue;
            player.NeedsReplication = false;
            if (!player.IsInWorld) continue;
            // movement：本轮要广播的最新确认移动快照。
            var movement = PlayerMessageMapper.ToMovement(player);
            // receiver：本条世界通知的接收玩家，用其 Gate 路由定向发送。
            foreach (var receiver in VisiblePlayers(online.Scene, player))
                online.Scene.Send(receiver.GateSceneAddress, new Game2G_PlayerMove
                {
                    GateSessionRuntimeId = receiver.GateSessionRuntimeId,
                    WorldSessionId = receiver.WorldSessionId,
                    RoleId = player.Id,
                    SpawnId = player.WorldSessionId,
                    SpawnRevision = player.SpawnRevision,
                    Version = player.MovementVersion,
                    ServerTimeMs = now,
                    Movement = movement,
                });
        }
    }
}
