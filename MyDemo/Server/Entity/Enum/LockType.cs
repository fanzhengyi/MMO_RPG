/// <summary>不同业务使用独立的协程锁，避免互相阻塞。</summary>
public enum LockType
{
    Account = 1,
    PlayerData = 2,
}
