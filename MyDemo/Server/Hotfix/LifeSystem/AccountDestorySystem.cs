using Fantasy.Entitas.Interface;

public class AccountDestorySystem : DestroySystem<Account>
{
    protected override void Destroy(Account self)
    {
        self.Username = "";
        self.Password = "";
        self.CreatTime = 0;
        self.LoginTime = 0;
    }
}