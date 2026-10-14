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
        var scene = session.Scene;
        var sessionRuntimeId = session.RuntimeId;
        response.Result = await scene.GetComponent<AuthenticationComponent>().Login(request.Username, request.Password);
        if (session.IsDisposed || session.RuntimeId != sessionRuntimeId) return;
        if (response.Result != ResultCode.Success) return;
        var gates = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate);
        if (gates.Count == 0)
        {
            response.Result = ResultCode.ServerError;
            return;
        }
        var gate = gates[(int)(HashCodeHelper.MurmurHash3(request.Username) % gates.Count)];
        var process = ProcessConfigData.Instance.Get(gate.ProcessConfigId);
        var address = MachineConfigData.Instance.Get(process.MachineId).OuterIP;
        response.GateAddress = $"{address}:{gate.OuterPort}";
        response.Token = scene.GetComponent<AuthenticationJwtComponent>()
            .GenerateJwtTokens(request.Username, gate.Id);
    }
}
