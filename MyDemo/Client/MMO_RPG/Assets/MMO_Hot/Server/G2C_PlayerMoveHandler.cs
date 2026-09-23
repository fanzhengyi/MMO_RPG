using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using UnityEngine;

/// <summary>
/// 服务器广播其他玩家移动，后续在这里刷新其他玩家的位置（先打印占位）。
/// </summary>
public class G2C_PlayerMoveHandler : Message<G2C_PlayerMove>
{
    protected override async FTask Run(Session session, G2C_PlayerMove message)
    {
        Debug.Log($"[移动同步] 玩家 RoleId={message.RoleId} 移动到 ({message.X},{message.Y},{message.Z}) 朝向={message.RotationY}");
        await FTask.CompletedTask;
    }
}
