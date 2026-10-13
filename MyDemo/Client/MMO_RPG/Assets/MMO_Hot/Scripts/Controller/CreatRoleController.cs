using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>请求角色数据并加载游戏场景，场景准备完成后才开启角色同步。</summary>
public sealed class CreatRoleController : Singleton<CreatRoleController>
{
    private const string GameSceneLocation = "Assets/MMO_Hot/Scenes/Scene Night.unity";
    private bool requesting;

    /// <summary>已有角色进入世界；无角色返回 NoRole，由界面选择下一步。</summary>
    public async FTask<AccountErrorCode> EnterGameAsync()
    {
        if (requesting) return AccountErrorCode.ServerError;
        requesting = true;
        try { return await EnterGameCoreAsync(); }
        catch (Exception exception) { Log.Error(exception); return AccountErrorCode.ServerError; }
        finally { requesting = false; }
    }

    /// <summary>创建成功后复用进入流程；nickname 是昵称，不是登录账号。</summary>
    public async FTask<AccountErrorCode> CreatRoleAsync(string nickname)
    {
        nickname = nickname?.Trim();
        if (string.IsNullOrEmpty(nickname) || nickname.Length > 24) return AccountErrorCode.NickNameNull;
        if (requesting) return AccountErrorCode.ServerError;
        var session = NetworkManager.Instance.GateSession;
        if (session == null || session.IsDisposed) return AccountErrorCode.NoLogin;
        var sessionRuntimeId = session.RuntimeId;
        requesting = true;
        try
        {
            AccountErrorCode result;
            using (var response = await session.C2G_CreateRoleRequest(nickname))
                result = (AccountErrorCode)response.AccountErrorCode;
            if (result != AccountErrorCode.CreateRoleSuccess && result != AccountErrorCode.HaveRole) return result;
            if (!NetworkManager.Instance.IsCurrentGate(session, sessionRuntimeId)) return AccountErrorCode.NoLogin;
            var entered = await EnterGameCoreAsync();
            return entered == AccountErrorCode.HaveRole ? AccountErrorCode.CreateRoleSuccess : entered;
        }
        catch (Exception exception) { Log.Error(exception); return AccountErrorCode.ServerError; }
        finally { requesting = false; }
    }

    /// <summary>复制响应后释放协议对象，再等待场景加载。</summary>
    private async FTask<AccountErrorCode> EnterGameCoreAsync()
    {
        var session = NetworkManager.Instance.GateSession;
        if (session == null || session.IsDisposed) return AccountErrorCode.NoLogin;
        var sessionRuntimeId = session.RuntimeId;
        PlayerSelfModel.Instance.IsInWorld = false;
        RemotePlayerManager.Reset();
        using (var response = await session.C2G_EnterGameRequest())
        {
            var result = (AccountErrorCode)response.AccountErrorCode;
            if (result != AccountErrorCode.HaveRole) return result;
            if (response.Info == null) return AccountErrorCode.ServerError;
            if (!NetworkManager.Instance.IsCurrentGate(session, sessionRuntimeId)) return AccountErrorCode.NoLogin;
            PlayerSelfModel.Instance.SetInfo(response.Info);
        }

        if (SceneManager.GetActiveScene().path != GameSceneLocation)
            await ResManager.Instance.LoadSceneAsync(GameSceneLocation);
        if (!NetworkManager.Instance.IsCurrentGate(session, sessionRuntimeId)) return AccountErrorCode.NoLogin;

        // 按组件找角色，不依赖场景物体恰好叫 PlayerRoot。
        var localPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (localPlayer == null) throw new InvalidOperationException("游戏场景中找不到 PlayerController。");
        var player = PlayerSelfModel.Instance;
        localPlayer.Place(new Vector3(player.X, player.Y, player.Z), player.RotationY);
        if (!RemotePlayerManager.Initialize(localPlayer.gameObject))
            return AccountErrorCode.ServerError;
        player.IsInWorld = true;
        localPlayer.SetControlEnabled(true);
        return AccountErrorCode.HaveRole;
    }
}
