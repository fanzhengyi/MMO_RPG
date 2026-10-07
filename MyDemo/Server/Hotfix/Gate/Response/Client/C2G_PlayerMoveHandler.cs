using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;

/// <summary>从 Gate Session 读取玩家身份，再把客户端移动快照转发到所属 Game。</summary>
public sealed class C2G_PlayerMoveHandler : Message<C2G_PlayerMove>
{
    protected override FTask Run(Session session, C2G_PlayerMove request)
    {
        var sessionInfo = session.GetComponent<SessionDisponse>();
        // 未登录或还没有 Game 路由的连接不能提交角色移动。
        if (sessionInfo == null || string.IsNullOrEmpty(sessionInfo.userName)
            || sessionInfo.gameSceneAddress == 0)
        {
            return FTask.CompletedTask;
        }

        // 身份和 Gate 路由都由服务端确定，客户端只提交位置、朝向、序号和动画状态。
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

        return FTask.CompletedTask;
    }
}
