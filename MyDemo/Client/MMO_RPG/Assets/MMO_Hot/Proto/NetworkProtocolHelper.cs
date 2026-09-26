using System.Runtime.CompilerServices;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using System.Collections.Generic;
#pragma warning disable CS8618
namespace Fantasy
{
   public static class NetworkProtocolHelper
   {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<A2C_RegisterResponse> C2A_RegisterRequest(this Session session, C2A_RegisterRequest C2A_RegisterRequest_request)
		{
			return (A2C_RegisterResponse)await session.Call(C2A_RegisterRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<A2C_RegisterResponse> C2A_RegisterRequest(this Session session, string username, string password)
		{
			using var C2A_RegisterRequest_request = Fantasy.C2A_RegisterRequest.Create();
			C2A_RegisterRequest_request.Username = username;
			C2A_RegisterRequest_request.Password = password;
			return (A2C_RegisterResponse)await session.Call(C2A_RegisterRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<A2C_LoginResponse> C2A_LoginRequest(this Session session, C2A_LoginRequest C2A_LoginRequest_request)
		{
			return (A2C_LoginResponse)await session.Call(C2A_LoginRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<A2C_LoginResponse> C2A_LoginRequest(this Session session, string username, string password)
		{
			using var C2A_LoginRequest_request = Fantasy.C2A_LoginRequest.Create();
			C2A_LoginRequest_request.Username = username;
			C2A_LoginRequest_request.Password = password;
			return (A2C_LoginResponse)await session.Call(C2A_LoginRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_LoginResponse> C2G_LoginRequest(this Session session, C2G_LoginRequest C2G_LoginRequest_request)
		{
			return (G2C_LoginResponse)await session.Call(C2G_LoginRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_LoginResponse> C2G_LoginRequest(this Session session, string token, string userName)
		{
			using var C2G_LoginRequest_request = Fantasy.C2G_LoginRequest.Create();
			C2G_LoginRequest_request.Token = token;
			C2G_LoginRequest_request.UserName = userName;
			return (G2C_LoginResponse)await session.Call(C2G_LoginRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G_2C_RepeaLogin(this Session session, G_2C_RepeaLogin G_2C_RepeaLogin_message)
		{
			session.Send(G_2C_RepeaLogin_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G_2C_RepeaLogin(this Session session)
		{
			using var message = Fantasy.G_2C_RepeaLogin.Create();
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_EnterGameResponse> C2G_EnterGameRequest(this Session session, C2G_EnterGameRequest C2G_EnterGameRequest_request)
		{
			return (G2C_EnterGameResponse)await session.Call(C2G_EnterGameRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_EnterGameResponse> C2G_EnterGameRequest(this Session session)
		{
			using var C2G_EnterGameRequest_request = Fantasy.C2G_EnterGameRequest.Create();
			return (G2C_EnterGameResponse)await session.Call(C2G_EnterGameRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateRoleResponse> C2G_CreateRoleRequest(this Session session, C2G_CreateRoleRequest C2G_CreateRoleRequest_request)
		{
			return (G2C_CreateRoleResponse)await session.Call(C2G_CreateRoleRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateRoleResponse> C2G_CreateRoleRequest(this Session session, string nickName)
		{
			using var C2G_CreateRoleRequest_request = Fantasy.C2G_CreateRoleRequest.Create();
			C2G_CreateRoleRequest_request.NickName = nickName;
			return (G2C_CreateRoleResponse)await session.Call(C2G_CreateRoleRequest_request);
		}

   }
}