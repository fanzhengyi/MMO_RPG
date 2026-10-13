using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas.Interface;
using Fantasy.MongdbModel;

public sealed class OnlineComponentAwakeSystem : AwakeSystem<OnlineComponent>
{
    /// <summary>启动角色定时保存。</summary>
    protected override void Awake(OnlineComponent self) => OnlinePlayerSystem.ScheduleSave(self);
}

public sealed class OnlineComponentDestroySystem : DestroySystem<OnlineComponent>
{
    /// <summary>关闭计时器并释放角色缓存。</summary>
    protected override void Destroy(OnlineComponent self)
    {
        if (self.SaveTimerId != 0) self.Scene.TimerComponent.Net.Remove(ref self.SaveTimerId);
        foreach (var player in self.PlayersByName.Values) player.Dispose();
        self.PlayersByName.Clear();
    }
}

/// <summary>角色加载、上线通知和持久化；调用方持有 PlayerData 锁。</summary>
public static class OnlinePlayerSystem
{
    /// <summary>优先复用内存角色，数据库实体加载后只激活一次。</summary>
    public static async FTask<OnlineInfo?> LoadAsync(Scene scene, string userName)
    {
        var online = scene.GetComponent<OnlineComponent>();
        if (online.PlayersByName.TryGetValue(userName, out var player)) return player;
        player = await scene.World.Database.First<OnlineInfo>(d => d.UserName == userName);
        if (player == null) return null;
        player.Deserialize(scene);
        online.PlayersByName[userName] = player;
        return player;
    }

    /// <summary>新客户端获取在线列表；只有换连接时才通知其他玩家。</summary>
    public static void SendPresence(Scene scene, OnlineInfo player, bool isNewConnection)
    {
        foreach (var other in scene.GetComponent<OnlineComponent>().PlayersByName.Values)
        {
            if (other.Id == player.Id || !other.IsConnected) continue;
            SendEntered(scene, player, other);
            if (isNewConnection) SendEntered(scene, other, player);
        }
    }

    /// <summary>向指定客户端发送另一个角色的初始快照。</summary>
    private static void SendEntered(Scene scene, OnlineInfo receiver, OnlineInfo player)
    {
        scene.Send(receiver.GateSceneAddress, new Game2G_PlayerEntered
        {
            GateSessionRuntimeId = receiver.GateSessionRuntimeId, RoleId = player.Id,
            UserName = player.UserName, NickName = player.NickName,
            X = player.X, Y = player.Y, Z = player.Z, RotationY = player.RotationY,
            State = player.MovementState, Speed = player.MovementSpeed, Sequence = player.MovementVersion,
        });
    }

    /// <summary>每五秒保存脏角色；上一轮结束后再安排下一轮，避免任务重叠。</summary>
    public static void ScheduleSave(OnlineComponent online)
    {
        if (online.IsDisposed) return;
        var runtimeId = online.RuntimeId;
        online.SaveTimerId = online.Scene.TimerComponent.Net.OnceTimer(5000, () =>
        {
            if (online.IsDisposed || online.RuntimeId != runtimeId) return;
            online.SaveTimerId = 0;
            FlushAsync(online).Coroutine();
        });
    }

    /// <summary>逐个保存角色；离线角色保存成功后释放，失败则下轮重试。</summary>
    private static async FTask FlushAsync(OnlineComponent online)
    {
        var runtimeId = online.RuntimeId;
        var scene = online.Scene;
        try
        {
            foreach (var player in online.PlayersByName.Values.ToArray())
            {
                var userName = player.UserName;
                var playerRuntimeId = player.RuntimeId;
                using (await scene.CoroutineLockComponent.Wait((int)LockType.PlayerData, userName.GetHashCode()))
                {
                    if (online.IsDisposed || online.RuntimeId != runtimeId) return;
                    if (player.IsDisposed || player.RuntimeId != playerRuntimeId) continue;
                    if (await SaveAsync(scene, player) && !player.IsConnected) Remove(online, player);
                }
            }
        }
        finally
        {
            if (!online.IsDisposed && online.RuntimeId == runtimeId) ScheduleSave(online);
        }
    }

    /// <summary>Fantasy Save 自带实体快照；保存期间的新移动留到下一轮写入。</summary>
    public static async FTask<bool> SaveAsync(Scene scene, OnlineInfo player)
    {
        if (!player.IsDirty) return true;
        var version = player.MovementVersion;
        try
        {
            await scene.World.Database.Save(player);
            if (player.MovementVersion == version) player.IsDirty = false;
            return true;
        }
        catch (Exception exception)
        {
            Log.Error($"角色保存失败，后续重试：{player.UserName}, {exception}");
            return false;
        }
    }

    /// <summary>保存完成后释放离线角色，避免在线表无限增长。</summary>
    public static void Remove(OnlineComponent online, OnlineInfo player)
    {
        online.PlayersByName.Remove(player.UserName);
        player.Dispose();
    }
}
