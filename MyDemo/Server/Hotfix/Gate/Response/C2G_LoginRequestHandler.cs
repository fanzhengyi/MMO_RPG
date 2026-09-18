using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;

public class C2G_LoginRequestHandler : MessageRPC<C2G_LoginRequest, G2C_LoginResponse>
{
    protected override async FTask Run(Session session, C2G_LoginRequest request, G2C_LoginResponse response, Action reply)
    {
        // 客户端必须先从 Authentication 获取 Token，再连接 Gate。
        if (string.IsNullOrEmpty(request.Token))
        {
            session.Dispose();
            return;
        }
        var gates = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate);
        // 根据用户名哈希计算客户端应该连接的 Gate。
        var gateIndex = HashCodeHelper.MurmurHash3(request.UserName) % gates.Count;
        var gateIdByUser = gates[(int)gateIndex].Id;

        // 同时验证 Token 签名和 Gate 路由，防止连接错误的 Gate。
        if (!GateJwtHelper.ValidateToken(session.Scene, request.Token) || gateIdByUser != session.Scene.SceneConfigId)
        {
            session.Dispose();
            return;
        }

        var cache = session.Scene.GetComponent<GateLoginCache>();
        if (!cache.TryGet(request.UserName, out var gameAccount))
        {
            // 第一次进入该 Gate，从数据库加载账号运行时数据。
            gameAccount = await GameAccountHelper.LoadDataBase(session.Scene, request.UserName);
            if (gameAccount == null)
            {
                gameAccount = await GameAccountFactory.CreateGameAccount(session.Scene, request.UserName);
            }

            cache.Add(gameAccount);
        }
        else
        {
            // 已经在线的账号可能是断线重连，也可能是重复登录或顶号。
            gameAccount.CancelTimeOut();
            if (session.RuntimeId == gameAccount.SessionRunTime)
            {
                // 同一个 Session 重复发送登录请求，直接返回。
                return;
            }

            if (session.Scene.TryGetEntity<Session>(gameAccount.SessionRunTime, out var oldSession))
            {
                // 顶号时通知旧客户端，并延迟销毁旧 Session。
                oldSession.GetComponent<SessionDisponse>().userName = "";
                oldSession.Send(new G_2C_RepeaLogin());
                oldSession.SetTimeout(3000);
            }
        }

        // 标记当前 Session 对应的账号，断开时由 SessionDisponse 处理下线。
        session.AddComponent<SessionDisponse>().userName = request.UserName;
        gameAccount.SessionRunTime = session.RuntimeId;
        response.LoginError = AccountErrorCode.LoginSuccess;
    }
}
