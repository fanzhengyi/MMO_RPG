// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
	/// <summary>
	/// 账号模板错误码
	/// </summary>
	public enum AccountErrorCode
	{
		/// <summary>
		/// 服务器内部错误
		/// </summary>
		ServerError = 0,
		/// <summary>
		/// 账号或者密码为空
		/// </summary>
		AccountPaawordEmpty = 1,
		/// <summary>
		/// 注册时账号存在
		/// </summary>
		RegisterAccountExist = 2,
		/// <summary>
		/// 注册成功
		/// </summary>
		RegisterSuccess = 3,
		/// <summary>
		/// 注册时这个账号不应该在这个鉴权服务器
		/// </summary>
		AuthenticationError = 4,
		/// <summary>
		/// 没有注册账号或者密码错误
		/// </summary>
		AccountNotExistOrPasswordError = 5,
		/// <summary>
		/// 登录成功
		/// </summary>
		LoginSuccess = 6,
		/// <summary>
		/// 有角色
		/// </summary>
		HaveRole = 7,
		/// <summary>
		/// 无角色
		/// </summary>
		NoRole = 8,
		/// <summary>
		/// 未登录
		/// </summary>
		NoLogin = 9
	}


}