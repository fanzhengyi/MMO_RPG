using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>给移动快照附上服务端确认的身份和路由，再单向发给 Game。</summary>
public sealed class C2G_PlayerMoveHandler : Message<C2G_PlayerMove>
{
    /// <summary>限频后附加服务端身份，将移动发给 Game。</summary>
    protected override async FTask Run(Session session, C2G_PlayerMove request)
    {
        if (!GateSessionHelper.TryGetPlayer(session, out var info))
            return;
        // 每条连接最多转发 50 次/秒，限制客户端连续切状态或恶意刷包。
        if (!session.CheckInterval(20)) return;

        session.Scene.Send(info.GameSceneAddress, new G2Game_PlayerMove
        {
            UserName = info.UserName,
            GateSessionRuntimeId = session.RuntimeId,
            GateSceneAddress = info.GateSceneAddress,
            X = request.X,
            Y = request.Y,
            Z = request.Z,
            RotationY = request.RotationY,
            Sequence = request.Sequence,
            State = request.State,
            Speed = request.Speed,
        });
        await FTask.CompletedTask;
    }
}
