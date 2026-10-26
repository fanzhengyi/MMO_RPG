using Fantasy.Entitas.Interface;

/// <summary>账号实体归还对象池时清除凭证和时间。</summary>
public sealed class AccountDestorySystem : DestroySystem<Account>
{
    /// <summary>防止对象池中的账号带着上一次的数据。</summary>
    protected override void Destroy(Account self)
    {
        self.AccountUser = "";
        self.Password = "";
        self.CreatTime = 0;
        self.LoginTime = 0;
    }
}
