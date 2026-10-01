using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;

/// <summary>验证客户端登录态，并把移动快照单向转发到所属 Game 场景。</summary>
public sealed class C2G_PlayerMoveHandler : Message<C2G_PlayerMove>
{
    protected override async FTask Run(Session session, C2G_PlayerMove request)
    {
        var sessionInfo = session.GetComponent<SessionDisponse>();
        if (sessionInfo == null || string.IsNullOrEmpty(sessionInfo.userName)
            || sessionInfo.gameSceneAddress == 0)
        {
            return;
        }

        // 用户名和 Gate 连接信息只从服务端 Session 读取，不接受客户端指定角色身份。
        var gateSceneAddress = SceneConfigData.Instance.Get(session.Scene.SceneConfigId).Address;
        session.Scene.Send(sessionInfo.gameSceneAddress, new G2Game_PlayerMove
        {
            UserName = sessionInfo.userName,
            GateSessionRuntimeId = session.RuntimeId,
            GateSceneAddress = gateSceneAddress,
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
