using Fantasy.Entitas;

public class SessionDisponse : Entity
{
    // Session 断开时，根据这个用户名安排 GameAccount 下线。
    public string userName;
}
