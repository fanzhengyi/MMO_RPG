namespace Fantasy.MongdbModel;
using Fantasy.Entitas;
public sealed class OnlineComponent : Entity
{
    // RoleId → 在线玩家
    public Dictionary<long, OnlineInfo> Players = new();
    // UserName → 在线玩家（反查用）
    public Dictionary<string, OnlineInfo> PlayersByName = new(StringComparer.Ordinal);
}
