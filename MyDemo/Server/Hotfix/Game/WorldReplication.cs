using Fantasy;
using Fantasy.MongdbModel;

/// <summary>统一管理世界可见性与复制；不校验输入，不访问数据库。</summary>
public static class WorldReplication
{
    private const int BroadcastIntervalMs = 50;

    public static void Start(OnlineComponent online)
    {
        var runtimeId = online.RuntimeId;
        online.ReplicationTimerId = online.Scene.TimerComponent.Net.RepeatedTimer(BroadcastIntervalMs, () =>
        {
            if (!online.IsDisposed && online.RuntimeId == runtimeId) Flush(online);
        });
    }

    /// <summary>集中定义当前可见范围；目前是一个公共场景，扩展 AOI 时替换此处。</summary>
    public static IEnumerable<OnlineInfo> VisiblePlayers(Scene scene, OnlineInfo observer)
    {
        foreach (var player in scene.GetComponent<OnlineComponent>().PlayersByName.Values)
            if (player.Id != observer.Id && player.IsInWorld) yield return player;
    }

    public static void PublishEntered(Scene scene, OnlineInfo player)
    {
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
        var now = Environment.TickCount64;
        foreach (var player in online.PlayersByName.Values)
        {
            if (!player.NeedsReplication) continue;
            player.NeedsReplication = false;
            if (!player.IsInWorld) continue;
            var movement = PlayerMessageMapper.ToMovement(player);
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
