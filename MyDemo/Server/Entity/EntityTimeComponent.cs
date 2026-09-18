using Fantasy.Entitas;

public class EntityTimeComponent : Entity
{
    public long TimeId;
    // 下一次能正常通讯的实际时间。
    public long NextTime;
    // 设置检测的间隔时间。
    public int IntervalTime;
}
