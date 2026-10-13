namespace Fantasy.MongdbModel;
using Fantasy.Entitas;
public sealed class OnlineComponent : Entity
{
    // 按账号定位角色；广播时直接遍历 Values。
    public Dictionary<string, OnlineInfo> PlayersByName = new(StringComparer.Ordinal);
}
