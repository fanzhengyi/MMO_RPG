/// <summary>
/// 玩家基础状态，协议同步时使用对应 int 值。
/// </summary>
public enum PlayerStateType
{
    Idle = 0,
    Move = 1,
    Jump = 2,
    Attack = 3,
    Skill = 4,
    Dead = 5,
    Run = 6,
    Fall = 7,
}
