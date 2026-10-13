using Fantasy.Entitas.Interface;

/// <summary>Session 释放时通知 Game 保存角色并移除在线状态。</summary>
public sealed class PlayerSessionDestroySystem : DestroySystem<PlayerSessionComponent>
{
    /// <summary>释放身份映射并发送离开通知。</summary>
    protected override void Destroy(PlayerSessionComponent self)
    {
        GateSessionHelper.Detach(self);
    }
}
