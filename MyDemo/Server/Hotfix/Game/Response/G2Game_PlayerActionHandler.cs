using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>Game 确认动作顺序、锁定与冷却，通过后立即发送独立动作事件。</summary>
public sealed class G2Game_PlayerActionHandler
    : AddressRPC<Scene, G2Game_PlayerActionRequest, Game2G_PlayerActionResponse>
{
    protected override async FTask Run(Scene scene, G2Game_PlayerActionRequest request,
        Game2G_PlayerActionResponse response, Action reply)
    {
        response.Sequence = request.Sequence;
        response.ServerTimeMs = Environment.TickCount64;
        response.Result = ResultCode.InvalidWorldSession;
        if (string.IsNullOrEmpty(request.AccountUser)) return;
        // online：本 Game 的在线角色索引；动作规则在场景线程内顺序执行，不访问数据库。
        var online = scene.GetComponent<OnlineComponent>();
        if (!online.PlayersByAccountUser.TryGetValue(request.AccountUser, out var player)
            || !player.IsInWorld
            || !player.MatchesWorld(request.GateSessionRuntimeId, request.GateSceneAddress, request.WorldSessionId)) return;
        response.Result = ResultCode.InvalidAction;
        if (request.MovementEpoch == player.MovementEpoch)
        {
            // decision：外部命令的动作时序结果，不直接信任客户端指定的动画进度和时长。
            var decision = PlayerActionRules.Process(player.Actions, request.Sequence,
                (PlayerActionId)request.Kind, request.RotationY, request.CancelSequence, response.ServerTimeMs);
            response.Result = decision switch
            {
                PlayerActionRules.Decision.Accepted => ResultCode.Success,
                PlayerActionRules.Decision.InvalidAction => ResultCode.InvalidAction,
                PlayerActionRules.Decision.Cooldown => ResultCode.Cooldown,
                _ => ResultCode.Busy,
            };
            if (decision == PlayerActionRules.Decision.Accepted)
            {
                if (request.Kind != PlayerActionKind.None)
                {
                    player.Actions.OriginX = player.X;
                    player.Actions.OriginY = player.Y;
                    player.Actions.OriginZ = player.Z;
                }
                WorldReplication.PublishAction(scene, player, response.ServerTimeMs);
            }
        }
        response.Action = PlayerMessageMapper.ToAction(player);
        await FTask.CompletedTask;
    }
}
