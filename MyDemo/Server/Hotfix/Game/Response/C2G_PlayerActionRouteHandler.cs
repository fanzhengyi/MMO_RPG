using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>玩家动作自定义路由；请求抵达绑定的 OnlineInfo 后直接执行共享动作规则。</summary>
public sealed class C2G_PlayerActionRouteHandler
    : AddressRPC<OnlineInfo, C2G_PlayerActionRequest, G2C_PlayerActionResponse>
{
    protected override async FTask Run(OnlineInfo player, C2G_PlayerActionRequest request,
        G2C_PlayerActionResponse response, Action reply)
    {
        response.Sequence = request.Sequence;
        response.ServerTimeMs = Environment.TickCount64;
        response.Result = ResultCode.InvalidWorldSession;
        if (!player.IsInWorld || request.WorldSessionId != player.WorldSessionId) return;

        response.Result = ResultCode.RateLimited;
        if (!player.TryAcceptAction(response.ServerTimeMs)) return;

        response.Result = ResultCode.InvalidAction;
        if (request.MovementEpoch == player.MovementEpoch)
        {
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
                WorldReplication.PublishAction(player.Scene, player, response.ServerTimeMs);
            }
        }

        response.Action = PlayerMessageMapper.ToAction(player);
        await FTask.CompletedTask;
    }
}
