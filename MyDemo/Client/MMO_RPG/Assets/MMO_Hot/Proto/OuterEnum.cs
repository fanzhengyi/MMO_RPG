// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
	/// <summary>
	/// 两端必须同步发布；缺少版本号的旧客户端在 Gate 被明确拒绝
	/// </summary>
	public enum ProtocolRevision
	{
		Unspecified = 0,
		Current = 3
	}

	/// <summary>
	/// 业务结果；默认值不是成功，Fantasy 的 ErrorCode 只表示框架错误
	/// </summary>
	public enum ResultCode
	{
		Unknown = 0,
		Success = 1,
		InvalidCredentials = 2,
		AccountExists = 3,
		WrongAuthenticationServer = 4,
		IncorrectCredentials = 5,
		InvalidToken = 6,
		NotAuthenticated = 7,
		RoleNotFound = 8,
		RoleAlreadyExists = 9,
		InvalidNickname = 10,
		ServerError = 11,
		Busy = 12,
		RateLimited = 13,
		UnsupportedProtocol = 14,
		InvalidWorldSession = 15
	}

	/// <summary>
	/// 网络移动状态，不使用 Animator 的参数编号
	/// </summary>
	public enum PlayerMotionState
	{
		Idle = 0,
		Walk = 1,
		Jump = 2,
		Run = 3,
		Fall = 4
	}


}