using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>Gate 提供可信身份，创建与昵称校验交给 Game。</summary>
public sealed class C2G_CreateRoleHandler : MessageRPC<C2G_CreateRoleRequest, G2C_CreateRoleResponse>
{
    /// <summary>转发创建请求；等待期间失效的连接不能继续完成请求。</summary>
    protected override async FTask Run(Session session, C2G_CreateRoleRequest request,
        G2C_CreateRoleResponse response, Action reply)
    {
        response.Result = ResultCode.NotAuthenticated;
        // info：网关绑定在该连接上的玩家身份、世界会话和路由信息。
        if (!GateSessionHelper.TryGetPlayer(session, out var info)) return;
        // runtimeId：开始操作时记录的对象运行时 ID，用于识别销毁或对象池复用。
        var runtimeId = session.RuntimeId;
        // result：Game 返回的创建角色响应，Gate 将结果转给客户端。
        using var result = (Game2G_CreateRoleResponse)await session.Scene.Call(
            info.GameSceneAddress, new G2Game_CreateRoleRequest
            {
                UserName = info.UserName,
                NickName = request.NickName,
            });
        if (session.RuntimeId != runtimeId || !GateSessionHelper.TryGetPlayer(session, out _)) return;
        response.Result = result == null || result.ErrorCode != 0 ? ResultCode.ServerError : result.Result;
    }
}
