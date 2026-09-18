using Fantasy.Entitas;

public sealed class GateLoginCache : Entity
{
    // Gate 内存缓存：用户名 -> 游戏账号运行时对象。
    // 该缓存不等于数据库，也不跨 Gate 进程共享。
    public Dictionary<string, GameAccount> gateAccountCache = new Dictionary<string, GameAccount>();
}
