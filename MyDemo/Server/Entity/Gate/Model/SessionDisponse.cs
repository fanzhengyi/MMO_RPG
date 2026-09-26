using Fantasy.Entitas;

public class SessionDisponse : Entity
{
    // Session 断开时，根据这个用户名安排 GameAccount 下线。
    public string userName;
    // 该玩家负责的 Game 场景地址（登录成功时算好缓存，转发时直接取）
    public long gameSceneAddress;
}
