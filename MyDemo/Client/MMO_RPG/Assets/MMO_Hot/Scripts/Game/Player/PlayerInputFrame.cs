using UnityEngine;

/// <summary>
/// 玩家一帧输入，后续会作为状态同步的上行数据。
/// </summary>
public struct PlayerInputFrame
{
    public int InputSeq;
    public int ClientTick;
    public Vector2 Move;
    public bool IsRun;
    public bool JumpPressed;
    public bool AttackPressed;
    public int SkillIndex;
    public float RotationY;

    public bool HasMove => Move.sqrMagnitude > 0.001f;
    public bool HasSkill => SkillIndex > 0;
}
