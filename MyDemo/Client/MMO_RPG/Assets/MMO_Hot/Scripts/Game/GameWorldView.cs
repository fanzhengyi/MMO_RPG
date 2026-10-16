using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>世界编号到客户端场景的映射及本地角色装配；不发送协议、不切换 UI。</summary>
public static class GameWorldView
{
    public const string GameSceneLocation = "Assets/MMO_Hot/Scenes/GameScene.unity";
    private static PlayerController localPlayer;

    public static async UniTask<PlayerController> PrepareAsync(PlayerSelfModel player)
    {
        if (player.WorldId != 1) throw new InvalidOperationException($"未配置的世界：{player.WorldId}");
        var worldSessionId = player.WorldSessionId;
        if (SceneManager.GetActiveScene().path != GameSceneLocation)
            await ResManager.Instance.LoadSceneAsync(GameSceneLocation);
        if (!player.AcceptsWorld(worldSessionId)) throw new OperationCanceledException("场景加载期间连接已失效。");

        localPlayer = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
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
