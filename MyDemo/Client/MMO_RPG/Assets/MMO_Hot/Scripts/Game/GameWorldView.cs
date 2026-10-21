using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>世界编号到客户端场景的映射及本地角色装配；不发送协议、不切换 UI。</summary>
public static class GameWorldView
{
    /// <summary>当前世界的 YooAsset 场景资源地址，不是服务器 HTTP 地址。</summary>
    public const string GameSceneLocation = "Assets/MMO_Hot/Scenes/GameScene.unity";
    /// <summary>当前场景内由本机输入控制的玩家；断线时暂停它。</summary>
    private static PlayerController localPlayer;

    // 只负责加载场景、找到本地控制器并放到出生点；返回时仍禁用输入，等待服务端 WorldReady 成功。
    public static async UniTask<PlayerController> PrepareAsync(PlayerSelfModel player)
    {
        if (player.WorldId != 1) throw new InvalidOperationException($"未配置的世界：{player.WorldId}");
        // worldSessionId：本次进入世界的会话 ID，用于排除旧世界的迟到操作。
        var worldSessionId = player.WorldSessionId;
        if (SceneManager.GetActiveScene().path != GameSceneLocation)
            await ResManager.Instance.LoadSceneAsync(GameSceneLocation);
        if (!player.AcceptsWorld(worldSessionId)) throw new OperationCanceledException("场景加载期间连接已失效。");

        localPlayer = null;
        // root：当前 Unity 游戏场景的一个根物体。
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        // candidate：场景层级中查找到的本地玩家控制器候选对象。
        foreach (var candidate in root.GetComponentsInChildren<PlayerController>(false))
        {
            if (localPlayer != null) throw new InvalidOperationException("游戏场景只能有一个本地 PlayerController。");
            localPlayer = candidate;
        }
        if (localPlayer == null) throw new InvalidOperationException("游戏场景中找不到 PlayerController。");
        localPlayer.SetControlEnabled(false);
        localPlayer.Place(new Vector3(player.X, player.Y, player.Z), player.RotationY);
        return localPlayer;
    }

    public static void Suspend()
    {
        if (localPlayer != null) localPlayer.SetControlEnabled(false);
        localPlayer = null;
    }
}
