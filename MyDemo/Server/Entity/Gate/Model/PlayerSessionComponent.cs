using Fantasy.Entitas;

/// <summary>当前客户端身份和路由；Session 销毁时用这份数据通知 Game。</summary>
public sealed class PlayerSessionComponent : Entity
{
    public string UserName = "";
    public long GameSceneAddress;
    public long GateSceneAddress;
    public long SessionRuntimeId;
}
