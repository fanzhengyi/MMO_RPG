/// <summary>状态基类，只保留生命周期和水平移动速度。</summary>
public abstract class PlayerState
{
    protected readonly PlayerFsmCore Machine;
    protected PlayerInputFrame Input => Machine.Input;
    public abstract PlayerStateType Type { get; }
    public virtual float MovementSpeed => 0f;

    /// <summary>保存当前角色的状态上下文。</summary>
    protected PlayerState(PlayerFsmCore machine)
    {
        Machine = machine;
    }

    /// <summary>进入状态时初始化本次行为。</summary>
    public virtual void Enter() { }

    /// <summary>推进本帧行为并判断是否需要切换状态。</summary>
    public abstract void Tick(float deltaTime);

    /// <summary>退出状态时清理本次行为。</summary>
    public virtual void Exit() { }
}
