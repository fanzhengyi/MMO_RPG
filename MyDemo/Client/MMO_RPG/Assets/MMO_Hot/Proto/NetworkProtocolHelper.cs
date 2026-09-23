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
		public static void G2C_PlayerInfoUpdate(this Session session, G2C_PlayerInfoUpdate G2C_PlayerInfoUpdate_message)
		{
			session.Send(G2C_PlayerInfoUpdate_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_PlayerInfoUpdate(this Session session, long roleId, long hp, long mp, long gold)
		{
			using var G2C_PlayerInfoUpdate_message = Fantasy.G2C_PlayerInfoUpdate.Create();
			G2C_PlayerInfoUpdate_message.RoleId = roleId;
			G2C_PlayerInfoUpdate_message.Hp = hp;
			G2C_PlayerInfoUpdate_message.Mp = mp;
			G2C_PlayerInfoUpdate_message.Gold = gold;
			session.Send(G2C_PlayerInfoUpdate_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_PlayerMove(this Session session, C2G_PlayerMove C2G_PlayerMove_message)
		{
			session.Send(C2G_PlayerMove_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_PlayerMove(this Session session, float x, float y, float z, float rotationY)
		{
			using var C2G_PlayerMove_message = Fantasy.C2G_PlayerMove.Create();
			C2G_PlayerMove_message.X = x;
			C2G_PlayerMove_message.Y = y;
			C2G_PlayerMove_message.Z = z;
			C2G_PlayerMove_message.RotationY = rotationY;
			session.Send(C2G_PlayerMove_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_PlayerMove(this Session session, G2C_PlayerMove G2C_PlayerMove_message)
		{
			session.Send(G2C_PlayerMove_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_PlayerMove(this Session session, long roleId, float x, float y, float z, float rotationY)
		{
			using var G2C_PlayerMove_message = Fantasy.G2C_PlayerMove.Create();
			G2C_PlayerMove_message.RoleId = roleId;
			G2C_PlayerMove_message.X = x;
			G2C_PlayerMove_message.Y = y;
			G2C_PlayerMove_message.Z = z;
			G2C_PlayerMove_message.RotationY = rotationY;
			session.Send(G2C_PlayerMove_message);
		}

   }
}