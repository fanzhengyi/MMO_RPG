using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;

/// <summary>验证账号后签发绑定账号和目标 Gate 的短期 Token。</summary>
public sealed class LoginResponse : MessageRPC<C2A_LoginRequest, A2C_LoginResponse>
{
    /// <summary>校验账号并返回目标 Gate 的短期凭证。</summary>
    protected override async FTask Run(Session session, C2A_LoginRequest request, A2C_LoginResponse response, Action reply)
    {
        response.Result = ResultCode.ServerError;
        if (!session.CheckInterval(500)) { response.Result = ResultCode.RateLimited; return; }
        // scene：当前操作所属的 Fantasy 网络场景，负责消息、实体和定时器。
        var scene = session.Scene;
        // sessionRuntimeId：开始操作时的连接运行时 ID，用于判断等待后是否仍是原连接。
        var sessionRuntimeId = session.RuntimeId;
        response.Result = await scene.GetComponent<AuthenticationComponent>().Login(request.Username, request.Password);
        if (session.IsDisposed || session.RuntimeId != sessionRuntimeId) return;
        if (response.Result != ResultCode.Success) return;
        // gates：配置表中的全部 Gate 场景，供账号哈希选择目标网关。
        var gates = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate);
        if (gates.Count == 0)
        {
            response.Result = ResultCode.ServerError;
            return;
        }
        // gate：本账号选中的 Gate 场景配置，不是网络连接对象。
        var gate = gates[(int)(HashCodeHelper.MurmurHash3(request.Username) % gates.Count)];
        // process：目标 Gate 所属的进程配置。
        var process = ProcessConfigData.Instance.Get(gate.ProcessConfigId);
        // address：目标 Gate 所在机器的客户端可访问地址。
        var address = MachineConfigData.Instance.Get(process.MachineId).OuterIP;
        response.GateAddress = $"{address}:{gate.OuterPort}";
        response.Token = scene.GetComponent<AuthenticationJwtComponent>()
            .GenerateJwtTokens(request.Username, gate.Id);
    }
}
