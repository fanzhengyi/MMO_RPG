using Fantasy;
using Fantasy.Entitas;
using Fantasy.Helper;
using System.Threading.Tasks;

public class GameAccountFactory
{
    // 创建一个新的 Gate 运行时账号，并按需保存到数据库。
    public static async Task<GameAccount> CreateGameAccount(Scene scene, string userName, bool IsSaveData = true)
    {
        GameAccount gameAccount = Entity.Create<GameAccount>(scene, true, true);
        gameAccount.UserName = userName;
        gameAccount.LoginTime = gameAccount.CreatTime = TimeHelper.Now;
        if (IsSaveData)
        {
            await gameAccount.SaveDataBase();
        }
        return gameAccount;
    }
}
