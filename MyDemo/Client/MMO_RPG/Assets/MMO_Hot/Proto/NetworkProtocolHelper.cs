using Fantasy.Async;
using Fantasy.Network;

namespace Fantasy
{
    /// <summary>客户端发送入口，只保留当前使用的参数形式；由项目维护。</summary>
    public static class NetworkProtocolHelper
    {
        // Create 的请求由 Fantasy 在发送后归还；不能跨 await 用 using 再次释放。
        /// <summary>向鉴权服请求注册账号。</summary>
        public static async FTask<A2C_RegisterResponse> C2A_RegisterRequest(this Session session, string username, string password)
        {
            var request = Fantasy.C2A_RegisterRequest.Create();
            request.Username = username;
            request.Password = password;
            return (A2C_RegisterResponse)await session.Call(request);
        }

        /// <summary>向鉴权服登录，获取 Gate Token。</summary>
        public static async FTask<A2C_LoginResponse> C2A_LoginRequest(this Session session, string username, string password)
        {
            var request = Fantasy.C2A_LoginRequest.Create();
            request.Username = username;
            request.Password = password;
            return (A2C_LoginResponse)await session.Call(request);
        }

        /// <summary>使用 Token 登录 Gate。</summary>
        public static async FTask<G2C_LoginResponse> C2G_LoginRequest(this Session session, string token)
        {
            var request = Fantasy.C2G_LoginRequest.Create();
            request.Token = token;
            return (G2C_LoginResponse)await session.Call(request);
        }

        /// <summary>请求进入世界并获取角色数据。</summary>
        public static async FTask<G2C_EnterGameResponse> C2G_EnterGameRequest(this Session session)
        {
            var request = Fantasy.C2G_EnterGameRequest.Create();
            return (G2C_EnterGameResponse)await session.Call(request);
        }

        /// <summary>按昵称创建角色。</summary>
        public static async FTask<G2C_CreateRoleResponse> C2G_CreateRoleRequest(this Session session, string nickName)
        {
            var request = Fantasy.C2G_CreateRoleRequest.Create();
            request.NickName = nickName;
            return (G2C_CreateRoleResponse)await session.Call(request);
        }

        /// <summary>上报位置、朝向和移动状态，不等待 RPC 响应。</summary>
        public static void C2G_PlayerMove(this Session session, uint sequence, MovementSnapshot movement)
        {
            var message = Fantasy.C2G_PlayerMove.Create();
            message.Sequence = sequence;
            message.Movement = movement;
            session.Send(message);
        }
    }
}
