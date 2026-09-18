using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;

public class LoginResponse : MessageRPC<C2A_LoginRequest, A2C_LoginResponse>
{
    protected override async FTask Run(Session session, C2A_LoginRequest request, A2C_LoginResponse response, Action reply)
    {
        // Authentication 验证账号密码，成功后才签发 Gate Token。
        var res = await AuthenticationHelper.Login(session.Scene, request.Username, request.Password);
        if (res == (int)AccountErrorCode.LoginSuccess)
        {
            // 根据账号哈希分配固定 Gate，保证同一账号总是进入同一组 Gate。
            var gates = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate);
            var gatePosition = HashCodeHelper.MurmurHash3(request.Username) % gates.Count;
            var gateConfig = gates[(int)gatePosition];
            var outPort = gateConfig.OuterPort;
            var process = ProcessConfigData.Instance.Get(gateConfig.ProcessConfigId);
            var address = MachineConfigData.Instance.Get(process.MachineId).OuterIP;
            // Token 内携带 Gate 地址，客户端据此建立长期 Gate 连接。
            string token = AuthenticationJwtHelper.GenerateToken(session.Scene, $"{address}:{outPort}");
            response.Token = token;
        }
        response.LoginError = (AccountErrorCode)res;
    }
}
