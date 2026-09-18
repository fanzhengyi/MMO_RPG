using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas.Interface;
using Fantasy.Network;

public sealed class GameAccountSystemDestory : DestroySystem<GameAccount>
{
    protected override void Destroy(GameAccount self)
    {
        self.UserName = "";
        self.CreatTime = 0;
        self.LoginTime = 0;
        self.SessionRunTime = 0;
    }
}

public static class GameAccountHelper
{
    // 从数据库读取 Gate 运行时账号对象。
    public static async FTask<GameAccount?> LoadDataBase(Scene scene, string userName)
    {
        var account = await scene.World.Database.First<GameAccount>(d => d.UserName == userName);
        if (account == null)
        {
            return null;
        }
        account.Deserialize(scene);
        return account;
    }

    public static async FTask Disconnect(this GameAccount self)
    {
        await SaveDataBase(self);
        self.Scene.GetComponent<GateLoginCache>().Remove(self.UserName);
    }

    public static async FTask Disconnect(Scene scene, string userName, int TimeOut = 1000 * 60 * 5)
    {
        // 断线后默认保留 5 分钟，期间重连可以复用原来的在线状态。
        var gateCache = scene.GetComponent<GateLoginCache>();
        if (!gateCache.TryGet(userName, out var account))
        {
            Log.Error("出错了");
            return;
        }
        if (!scene.TryGetEntity<Session>(account.SessionRunTime, out var session))
        {
            Log.Error("出错了");
            return;
        }
        if (account.IsHasTimeComponent())
        {
            return;
        }
        if (TimeOut <= 0)
        {
            await account.Disconnect();
        }
        else
        {
            account.SetTimeout(TimeOut, account.Disconnect);
        }
    }

    public static async FTask SaveDataBase(this GameAccount self)
    {
        await self.Scene.World.Database.Save(self);
    }
}
