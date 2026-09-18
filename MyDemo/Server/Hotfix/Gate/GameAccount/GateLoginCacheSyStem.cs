using Fantasy.Entitas.Interface;

public sealed class GateLoginCacheSystemDestory : DestroySystem<GateLoginCache>
{
    protected override void Destroy(GateLoginCache self)
    {
        foreach (var (_, account) in self.gateAccountCache)
        {
            account.Dispose();
        }
        self.gateAccountCache.Clear();
    }
}

public static class GateLoginCacheSyStem
{
    // 将账号运行时对象放入 Gate 内存缓存。
    public static void Add(this GateLoginCache self, GameAccount account)
    {
        self.gateAccountCache.Add(account.UserName, account);
    }

    public static bool TryGet(this GateLoginCache self, string userName, out GameAccount? account)
    {
        return self.gateAccountCache.TryGetValue(userName, out account);
    }

    public static void Remove(this GateLoginCache self, string userName)
    {
        if (!self.gateAccountCache.Remove(userName, out var account))
        {
            return;
        }
        account.Dispose();
    }
}
