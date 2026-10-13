using Fantasy.Entitas;
namespace Fantasy.MongdbModel;

/// <summary>当前 Game 的在线角色索引和定时保存任务。</summary>
public sealed class OnlineComponent : Entity
{
    public readonly Dictionary<string, OnlineInfo> PlayersByName = new(StringComparer.Ordinal);
    public long SaveTimerId;
}
