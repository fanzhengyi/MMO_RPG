using Fantasy.Entitas;

public class LoginCacheEntity : Entity
{
    // 该实体本身不保存账号数据，只作为登录缓存的容器。
    // 成功时挂载 Account，失败时不挂载 Account。
}
