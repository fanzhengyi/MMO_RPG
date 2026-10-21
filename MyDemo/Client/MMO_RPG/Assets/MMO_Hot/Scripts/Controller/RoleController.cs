using System;
using Fantasy;
using Fantasy.Async;

/// <summary>请求角色数据并加载游戏场景，场景准备完成后才开启角色同步。</summary>
public sealed class RoleController : Singleton<RoleController>
{
    /// <summary>创建角色和进入世界共用的流程占用标记，防止同时装配两次世界；在 finally 释放。</summary>
    private bool requesting;

    /// <summary>已有角色进入世界；没有角色由界面转到创建流程。</summary>
    public async FTask<ResultCode> EnterGameAsync()
    {
        if (requesting) return ResultCode.Busy;
        requesting = true;
        try { return await EnterGameCoreAsync(); }
        // exception：本次操作抛出的异常，用于记录具体失败原因。
        catch (Exception exception) { Log.Error(exception); return ResultCode.ServerError; }
        finally { requesting = false; }
    }

    /// <summary>创建成功后复用进入流程；nickname 是昵称，不是登录账号。</summary>
    public async FTask<ResultCode> CreateRoleAsync(string nickname)
    {
        nickname = nickname?.Trim();
        if (string.IsNullOrEmpty(nickname) || nickname.Length > 24) return ResultCode.InvalidNickname;
        if (requesting) return ResultCode.Busy;
        // session：本次操作使用的客户端网络连接。
        var session = NetworkManager.Instance.GateSession;
        if (session == null || session.IsDisposed) return ResultCode.NotAuthenticated;
        // sessionRuntimeId：开始操作时的连接运行时 ID，用于判断等待后是否仍是原连接。
        var sessionRuntimeId = session.RuntimeId;
        requesting = true;
        try
        {
            // result：创建角色、预备进入或世界就绪请求的业务结果，具体取决于当前步骤。
            ResultCode result;
            // response：本次请求返回的响应对象，using 结束后交回协议对象池。
            using (var response = await session.C2G_CreateRoleRequest(nickname))
                result = response.ErrorCode != 0 ? ResultCode.ServerError : response.Result;
            if (result != ResultCode.Success && result != ResultCode.RoleAlreadyExists) return result;
            if (!NetworkManager.Instance.IsCurrentGate(session, sessionRuntimeId)) return ResultCode.NotAuthenticated;
            return await EnterGameCoreAsync();
        }
        // exception：本次操作抛出的异常，用于记录具体失败原因。
        catch (Exception exception) { Log.Error(exception); return ResultCode.ServerError; }
        finally { requesting = false; }
    }

    /// <summary>复制响应后释放协议对象，再等待场景加载。</summary>
    private async FTask<ResultCode> EnterGameCoreAsync()
    {
        // session：本次操作使用的客户端网络连接。
        var session = NetworkManager.Instance.GateSession;
        if (session == null || session.IsDisposed) return ResultCode.NotAuthenticated;
        // sessionRuntimeId：开始操作时的连接运行时 ID，用于判断等待后是否仍是原连接。
        var sessionRuntimeId = session.RuntimeId;
        if (PlayerSelfModel.Instance.WorldSessionId != 0) return ResultCode.Busy;
        PlayerSelfModel.Instance.IsInWorld = false;
        RemotePlayerManager.Reset();
        // entered 表示客户端最终进入成功；prepared 表示服务端已给本次进入预留世界会话，两者不是同一阶段。
        // entered：客户端是否已完成世界就绪并开放控制，作为最终成功标记。
        var entered = false;
        // prepared：服务端是否已接受进入请求并预留世界会话，失败时可能需要撤销。
        var prepared = false;
        try
        {
            // 第一阶段：询问角色并取得出生数据、WorldSessionId；此时服务端还没有向其他玩家发布上线。
            // response：本次请求返回的响应对象，using 结束后交回协议对象池。
            using (var response = await session.C2G_EnterGameRequest())
            {
                // result：创建角色、预备进入或世界就绪请求的业务结果，具体取决于当前步骤。
                var result = response.ErrorCode != 0 ? ResultCode.ServerError : response.Result;
                if (result != ResultCode.Success) return result;
                prepared = true;
                if (response.Info?.Movement == null || response.Info.RoleId == 0 || response.WorldSessionId == 0)
                    return ResultCode.ServerError;
                if (!NetworkManager.Instance.IsCurrentGate(session, sessionRuntimeId)) return ResultCode.NotAuthenticated;
                PlayerSelfModel.Instance.SetInfo(response.Info, response.WorldSessionId, response.WorldId);
            }

            // player：本人的角色数据模型，用来复制进入响应和记录世界会话。
            var player = PlayerSelfModel.Instance;
            // worldSessionId：本次进入世界的会话 ID，用于排除旧世界的迟到操作。
            var worldSessionId = player.WorldSessionId;
            // 第二阶段：加载 Unity 场景并将本地角色放到出生点，完成前不开放移动输入。
            // localPlayer：游戏场景里由本机输入控制的角色控制器。
            var localPlayer = await GameWorldView.PrepareAsync(player);
            if (!NetworkManager.Instance.IsCurrentGate(session, sessionRuntimeId)) return ResultCode.NotAuthenticated;

            RemotePlayerManager.Initialize(localPlayer);
            // 第三阶段：告诉服务端本地已准备好，成功后获取其他玩家基线并正式参与世界同步。
            // response：本次请求返回的响应对象，using 结束后交回协议对象池。
            using (var response = await session.C2G_WorldReadyRequest(worldSessionId))
            {
                if (!NetworkManager.Instance.IsCurrentGate(session, sessionRuntimeId)
                    || !player.AcceptsWorld(worldSessionId)) return ResultCode.NotAuthenticated;
                // result：创建角色、预备进入或世界就绪请求的业务结果，具体取决于当前步骤。
                var result = response.ErrorCode != 0 ? ResultCode.ServerError : response.Result;
                if (result != ResultCode.Success) return result;
                // other：当前遍历到的另一个可见玩家。
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
