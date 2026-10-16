using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas.Interface;
using Fantasy.MongdbModel;

public sealed class OnlineComponentAwakeSystem : AwakeSystem<OnlineComponent>
{
    /// <summary>启动角色定时保存。</summary>
    protected override void Awake(OnlineComponent self)
    {
        OnlinePlayerSystem.ScheduleSave(self);
        WorldReplication.Start(self);
    }
}

public sealed class OnlineComponentDestroySystem : DestroySystem<OnlineComponent>
{
    /// <summary>关闭计时器并释放角色缓存。</summary>
    protected override void Destroy(OnlineComponent self)
    {
        if (self.SaveTimerId != 0) self.Scene.TimerComponent.Net.Remove(ref self.SaveTimerId);
        if (self.ReplicationTimerId != 0) self.Scene.TimerComponent.Net.Remove(ref self.ReplicationTimerId);
        foreach (var player in self.PlayersByName.Values) player.Dispose();
        self.PlayersByName.Clear();
    }
}

/// <summary>角色加载和持久化；调用方持有 PlayerData 锁，不负责世界广播。</summary>
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
