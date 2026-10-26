using System;
using Fantasy;
using Fantasy.Async;
using UnityEngine;

/// <summary>动作的网络适配：本地立即表现，服务端拒绝时只撤销对应的那次预测。</summary>
[RequireComponent(typeof(PlayerController))]
[DefaultExecutionOrder(210)]
public sealed class PlayerActionSync : MonoBehaviour
{
    private PlayerController player; // 提供动作事件的本地控制器。
    private uint activeRequestSequence; // 最近一次出招的网络序号，用于精确取消。

    private void OnEnable()
    {
        player = GetComponent<PlayerController>();
        player.ActionChanged += OnActionChanged;
    }

    private void OnDisable() => player.ActionChanged -= OnActionChanged;

    private void OnActionChanged(PlayerActionId id, uint generation) => SendAsync(id, generation).Coroutine();

    private async FTask SendAsync(PlayerActionId id, uint generation)
    {
        var session = NetworkManager.Instance.GateSession; // 此次请求绑定的 Gate，不能在等待后换用新连接。
        if (session == null || session.IsDisposed || !PlayerSelfModel.Instance.IsInWorld) return;
        var runtimeId = session.RuntimeId; // Session 对象可能池化，记录运行身份。
        var world = PlayerSelfModel.Instance.WorldSessionId;
        var epoch = PlayerSelfModel.Instance.MovementEpoch;
        var sequence = NetworkManager.Instance.NextActionSequence();
        var cancel = id == PlayerActionId.None ? activeRequestSequence : 0;
        if (id != PlayerActionId.None) activeRequestSequence = sequence;
        try
        {
            using (var response = await session.C2G_PlayerActionRequest(world, sequence, epoch,
                (PlayerActionKind)(int)id, transform.eulerAngles.y, cancel))
            {
                if (!NetworkManager.Instance.IsCurrentGate(session, runtimeId)
                    || !PlayerSelfModel.Instance.AcceptsWorld(world) || PlayerSelfModel.Instance.MovementEpoch != epoch) return;
                if (response.Result != ResultCode.Success && id != PlayerActionId.None)
                    player.RejectAction(generation);
            }
        }
        catch (Exception exception)
        {
            if (NetworkManager.Instance.IsCurrentGate(session, runtimeId) && PlayerSelfModel.Instance.AcceptsWorld(world))
                player.RejectAction(generation);
            Debug.LogException(exception, this);
        }
    }
}
