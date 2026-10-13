using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>Gate 验证连接身份，Game 负责角色创建和数据保存。</summary>
public sealed class C2G_CreateRoleHandler : MessageRPC<C2G_CreateRoleRequest, G2C_CreateRoleResponse>
{
    /// <summary>核对身份和昵称，转发创建请求并映射结果。</summary>
    protected override async FTask Run(Session session, C2G_CreateRoleRequest request,
        G2C_CreateRoleResponse response, Action reply)
    {
        response.AccountErrorCode = (int)AccountErrorCode.NoLogin;
        if (!GateSessionHelper.TryGetPlayer(session, out var info))
            return;
        if (string.IsNullOrWhiteSpace(request.NickName) || request.NickName.Trim().Length > 24)
        {
            response.AccountErrorCode = (int)AccountErrorCode.NickNameNull;
            return;
        }

        var sessionRuntimeId = session.RuntimeId;
        using var gameResponse = (Game2G_CreateRoleResponse)await session.Scene.Call(
            info.GameSceneAddress, new G2Game_CreateRoleRequest
            {
                UserName = info.UserName,
                NickName = request.NickName.Trim(),
                GateSessionRuntimeId = session.RuntimeId,
                GateSceneAddress = info.GateSceneAddress,
            });

        if (session.RuntimeId != sessionRuntimeId || !GateSessionHelper.TryGetPlayer(session, out _))
            return;
        if (gameResponse == null)
        {
            response.AccountErrorCode = (int)AccountErrorCode.ServerError;
            return;
        }

        response.AccountErrorCode = gameResponse.AccountErrorCode;
        if (gameResponse.AccountErrorCode == (int)AccountErrorCode.CreateRoleSuccess
            || gameResponse.AccountErrorCode == (int)AccountErrorCode.HaveRole)
        {
            if (gameResponse.Data == null)
            {
                response.AccountErrorCode = (int)AccountErrorCode.ServerError;
                return;
            }
            response.Info = PlayerMessageMapper.ToPlayerInfo(gameResponse.Data);
        }
    }
}
