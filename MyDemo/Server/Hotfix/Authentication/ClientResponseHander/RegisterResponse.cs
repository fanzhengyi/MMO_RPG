using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

public class RegisterResponse : MessageRPC<C2A_RegisterRequest, A2C_RegisterResponse>
{
    protected override async FTask Run(Session session, C2A_RegisterRequest request, A2C_RegisterResponse response, Action reply)
    {
        // 限制同一个连接的注册频率，避免重复注册请求消耗数据库资源。
        if (!session.CheakInterval(2000))
        {
            return;
        }
        response.LoginError = await AuthenticationHelper.Resgister(session.Scene, request.Username, request.Password);
        // 注册是短连接，响应发送后延迟关闭 Session。
        session.SetTimeout(3000);
    }
}
