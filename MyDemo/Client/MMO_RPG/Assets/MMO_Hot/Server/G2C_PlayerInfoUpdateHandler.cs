using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using UnityEngine;

/// <summary>
/// 服务器推送玩家状态变化（血量/法力/金币），后续在这里刷新UI。
/// </summary>
public class G2C_PlayerInfoUpdateHandler : Message<G2C_PlayerInfoUpdate>
{
    protected override async FTask Run(Session session, G2C_PlayerInfoUpdate message)
    {
        Debug.Log($"[状态同步] 玩家数据变化 RoleId={message.RoleId} Hp={message.Hp} Mp={message.Mp} Gold={message.Gold}");
        await FTask.CompletedTask;
    }
}
