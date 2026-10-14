using System;
using Fantasy;
using Fantasy.Async;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>请求角色数据并加载游戏场景，场景准备完成后才开启角色同步。</summary>
public sealed class RoleController : Singleton<RoleController>
{
    private const string GameSceneLocation = "Assets/MMO_Hot/Scenes/Scene Night.unity";
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
        PlayerSelfModel.Instance.IsInWorld = false;
        RemotePlayerManager.Reset();
        using (var response = await session.C2G_EnterGameRequest())
        {
            var result = response.ErrorCode != 0 ? ResultCode.ServerError : response.Result;
            if (result != ResultCode.Success) return result;
            if (response.Info?.Movement == null || response.Info.RoleId == 0) return ResultCode.ServerError;
            if (!NetworkManager.Instance.IsCurrentGate(session, sessionRuntimeId)) return ResultCode.NotAuthenticated;
            PlayerSelfModel.Instance.SetInfo(response.Info);
        }

        if (SceneManager.GetActiveScene().path != GameSceneLocation)
            await ResManager.Instance.LoadSceneAsync(GameSceneLocation);
        if (!NetworkManager.Instance.IsCurrentGate(session, sessionRuntimeId)) return ResultCode.NotAuthenticated;

        // 按组件找角色，不依赖场景物体恰好叫 PlayerRoot。
        var localPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (localPlayer == null) throw new InvalidOperationException("游戏场景中找不到 PlayerController。");
        var player = PlayerSelfModel.Instance;
        localPlayer.Place(new Vector3(player.X, player.Y, player.Z), player.RotationY);
        if (!RemotePlayerManager.Initialize(localPlayer.gameObject))
            return ResultCode.ServerError;
        player.IsInWorld = true;
        localPlayer.SetControlEnabled(true);
        return ResultCode.Success;
    }
}
