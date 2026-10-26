using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>进入请求交给 Game，Gate 不访问角色数据库。</summary>
public sealed class C2G_EnterGameHandler : MessageRPC<C2G_EnterGameRequest, G2C_EnterGameResponse>
{
    /// <summary>核对连接，转发进入请求并返回角色 DTO。</summary>
    protected override async FTask Run(Session session, C2G_EnterGameRequest request,
        G2C_EnterGameResponse response, Action reply)
    {
        response.Result = ResultCode.NotAuthenticated;
        // info：网关绑定在该连接上的玩家身份、世界会话和路由信息。
        if (!GateSessionHelper.TryGetPlayer(session, out var info)) return;
        if (info.IsEntering || info.WorldSessionId != 0) { response.Result = ResultCode.Busy; return; }
        //设置进入游戏
        info.IsEntering = true;
        
        // runtimeId：开始操作时记录的对象运行时 ID，用于识别销毁或对象池复用。
        var runtimeId = session.RuntimeId;
        // componentRuntimeId：等待前的玩家会话组件运行时 ID，避免清理时误操作复用后的组件。
        var componentRuntimeId = info.RuntimeId;
        
        try
        {
            // result：Game 返回的进入世界响应，含角色资料和世界会话。
            using var result = (Game2G_EnterGameResponse)await session.Scene.Call(
                info.GameSceneAddress, new G2Game_EnterGameRequest
                {
                    AccountUser = info.AccountUser,
                    GateSessionRuntimeId = runtimeId,
                    GateSceneAddress = info.GateSceneAddress,
                });
            
            if (session.RuntimeId != runtimeId || !GateSessionHelper.TryGetPlayer(session, out _)) return;
            
            if (result == null || result.ErrorCode != 0
                || (result.Result == ResultCode.Success && (result.Info?.Movement == null
                    || result.WorldSessionId == 0 || result.PlayerAddress == 0)))
            {
                response.Result = ResultCode.ServerError;
                return;
            }
            response.Result = result.Result;
            response.Info = result.Info;
            response.WorldSessionId = result.WorldSessionId;
            response.WorldId = result.WorldId;
            if (result.Result == ResultCode.Success)
            {
                info.WorldSessionId = result.WorldSessionId;
                GateSessionHelper.BindPlayerRoute(session, result.PlayerAddress);
            }
        }
        finally
        {
            if (!info.IsDisposed && info.RuntimeId == componentRuntimeId) info.IsEntering = false;
        }
    }
}
