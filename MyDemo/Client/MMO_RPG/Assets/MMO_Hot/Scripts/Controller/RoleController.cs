using System;
using Fantasy;
using Fantasy.Async;

/// <summary>请求角色数据并加载游戏场景，场景准备完成后才开启角色同步。</summary>
public sealed class RoleController : Singleton<RoleController>
{
    private bool requesting;

    /// <summary>已有角色进入世界；没有角色由界面转到创建流程。</summary>
    public async FTask<ResultCode> EnterGameAsync()
    {
        if (requesting) return ResultCode.Busy;
        requesting = true;
        try { return await EnterGameCoreAsync(); }
        catch (Exception exception) { Log.Error(exception); return ResultCode.ServerError; }
        finally { requesting = false; }
    }

    /// <summary>创建成功后复用进入流程；nickname 是昵称，不是登录账号。</summary>
    public async FTask<ResultCode> CreateRoleAsync(string nickname)
    {
        nickname = nickname?.Trim();
        if (string.IsNullOrEmpty(nickname) || nickname.Length > 24) return ResultCode.InvalidNickname;
        if (requesting) return ResultCode.Busy;
        var session = NetworkManager.Instance.GateSession;
        if (session == null || session.IsDisposed) return ResultCode.NotAuthenticated;
        var sessionRuntimeId = session.RuntimeId;
        requesting = true;
        try
        {
            ResultCode result;
            using (var response = await session.C2G_CreateRoleRequest(nickname))
                result = response.ErrorCode != 0 ? ResultCode.ServerError : response.Result;
            if (result != ResultCode.Success && result != ResultCode.RoleAlreadyExists) return result;
            if (!NetworkManager.Instance.IsCurrentGate(session, sessionRuntimeId)) return ResultCode.NotAuthenticated;
            return await EnterGameCoreAsync();
        }
        catch (Exception exception) { Log.Error(exception); return ResultCode.ServerError; }
        finally { requesting = false; }
    }

    /// <summary>复制响应后释放协议对象，再等待场景加载。</summary>
    private async FTask<ResultCode> EnterGameCoreAsync()
    {
        var session = NetworkManager.Instance.GateSession;
        if (session == null || session.IsDisposed) return ResultCode.NotAuthenticated;
        var sessionRuntimeId = session.RuntimeId;
        if (PlayerSelfModel.Instance.WorldSessionId != 0) return ResultCode.Busy;
        PlayerSelfModel.Instance.IsInWorld = false;
        RemotePlayerManager.Reset();
        var entered = false;
        var prepared = false;
        try
        {
            using (var response = await session.C2G_EnterGameRequest())
            {
                var result = response.ErrorCode != 0 ? ResultCode.ServerError : response.Result;
                if (result != ResultCode.Success) return result;
                prepared = true;
                if (response.Info?.Movement == null || response.Info.RoleId == 0 || response.WorldSessionId == 0)
                    return ResultCode.ServerError;
                if (!NetworkManager.Instance.IsCurrentGate(session, sessionRuntimeId)) return ResultCode.NotAuthenticated;
                PlayerSelfModel.Instance.SetInfo(response.Info, response.WorldSessionId, response.WorldId);
            }

            var player = PlayerSelfModel.Instance;
            var worldSessionId = player.WorldSessionId;
            var localPlayer = await GameWorldView.PrepareAsync(player);
            if (!NetworkManager.Instance.IsCurrentGate(session, sessionRuntimeId)) return ResultCode.NotAuthenticated;

            RemotePlayerManager.Initialize(localPlayer);
            using (var response = await session.C2G_WorldReadyRequest(worldSessionId))
            {
                if (!NetworkManager.Instance.IsCurrentGate(session, sessionRuntimeId)
                    || !player.AcceptsWorld(worldSessionId)) return ResultCode.NotAuthenticated;
                var result = response.ErrorCode != 0 ? ResultCode.ServerError : response.Result;
                if (result != ResultCode.Success) return result;
                foreach (var other in response.Players)
                    if (other?.Movement != null) RemotePlayerManager.PlayerEntered(RemotePlayerManager.Snapshot.From(other));
            }
            player.IsInWorld = true;
            localPlayer.SetControlEnabled(true);
            entered = true;
            return ResultCode.Success;
        }
        finally
        {
            // 准备成功后发生资源/就绪失败，需要撤销服务端占位，允许重新登录。
            if (!entered && NetworkManager.Instance.IsCurrentGate(session, sessionRuntimeId)
                && prepared) NetworkManager.Instance.DisconnectGate();
        }
    }
}
