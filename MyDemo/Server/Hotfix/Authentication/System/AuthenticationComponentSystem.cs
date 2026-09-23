using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
using Fantasy.Platform.Net;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

public sealed class AuthenticationComponentSystemDestory : DestroySystem<AuthenticationComponent>
{
    protected override void Destroy(AuthenticationComponent self)
    {
        // 场景销毁时释放两个缓存字典中的实体，避免对象池对象残留。
        foreach (var (_, RegisterAccount) in self.RegisterCacheDic)
        {
            RegisterAccount.Dispose();
        }
        foreach (var (_, loginAccount) in self.LoginCacheDic)
        {
            loginAccount.Dispose();
        }
        self.LoginCacheDic.Clear();
        self.RegisterCacheDic.Clear();
    }
}

//1参数不完整，2账号已存在，3.表示当前没有到对应鉴权服务器。0操作成功
public static class AuthenticationComponentSystem
{
    // 更新当前鉴权服务器在配置表中的位置和总数
    public static void UpdateServerData(this AuthenticationComponent self)
    {
        // 所有鉴权服务器使用同一套配置，账号哈希后只由其中一台处理。
        var authenSevers = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Authentication);
        //拿到当前sceneId
        var CurSceneData = SceneConfigData.Instance.Get(self.Scene.SceneConfigId);
        self.LocalServerConfigIndex = authenSevers.IndexOf(CurSceneData);
        self.AuthernServerCount = authenSevers.Count;
    }

    //返回错误码和（账号id加入JWT给服务器验证)
    public static async FTask<uint> Login(this AuthenticationComponent self, string username, string password)
    {
        // 先做参数校验，非法请求不访问数据库。
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return (int)AccountErrorCode.AccountPaawordEmpty;//参数不完整
        }

        // 根据用户名计算负责该账号的鉴权服务器。
        var clientPosition = HashCodeHelper.MurmurHash3(username) % self.AuthernServerCount;
        //不属于当前服务器
        if (self.LocalServerConfigIndex != clientPosition)
        {
            return (int)AccountErrorCode.AuthenticationError;
        }

        var scene = self.Scene;
        var wordDataBase = scene.World.Database;
        // 同一个账号的请求串行执行，避免并发登录造成缓存击穿。
        using (var @lock = await scene.CoroutineLockComponent.Wait((int)LockType.LoginLock, username.GetHashCode()))
        {
            Account account = null;
            // 使用账号和密码作为键：输错密码不会影响随后输入正确密码的请求。
            var loginAccountsKey = username + password;
            if (self.LoginCacheDic.TryGetValue(loginAccountsKey, out var loginCache))
            {
                // 缓存中没有 Account 表示上一次验证失败。
                account = loginCache.GetComponent<Account>();
                if (account == null)
                {
                    //账号或者密码错误或者没有注册
                    return (int)AccountErrorCode.AccountNotExistOrPasswordError;
                }
                else
                {
                    return (int)AccountErrorCode.LoginSuccess;//账号已经登录
                }
            }
            // 缓存未命中，才访问数据库。
            loginCache = Entity.Create<LoginCacheEntity>(scene, true, true);
            account = await wordDataBase.First<Account>(d => d.Username == username && d.Password == password);
            uint res = 0;
            if (account == null)
            {
                // 账号不存在或密码错误，也写入短时缓存，防止相同请求反复查库。
                res = (int)AccountErrorCode.AccountNotExistOrPasswordError;
            }
            else
            {
                // 登录成功后更新数据库中的最近登录时间。
                account.LoginTime = TimeHelper.Now;
                await wordDataBase.Save(account);
                account.Deserialize(scene);
                loginCache.AddComponent(account);
            }
            // 失败时没有 Account 可挂载，但 LoginCacheEntity 仍然可以记录这次失败。
            self.LoginCacheDic.Add(loginAccountsKey, loginCache);
            // 4 秒后由 LoginTimeClear 移除该缓存。
            loginCache.AddComponent<LoginTimeClear>().TimeOut(loginAccountsKey, 4000);
            if (res != 0)//account为空
            {
                return res;
            }
            return (int)AccountErrorCode.LoginSuccess;
        }
    }

    //注册验证
    public static async FTask<AccountErrorCode> Resgister(this AuthenticationComponent self, string username, string password)
    {
        // 注册也必须先校验账号和密码。
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return AccountErrorCode.AccountPaawordEmpty;
        }
        var clientPosition = HashCodeHelper.MurmurHash3(username) % self.AuthernServerCount;
        // 当前服务器不是该账号的负责服务器。
        if (self.LocalServerConfigIndex != clientPosition)
        {
            return AccountErrorCode.AuthenticationError;
        }

        // 协程锁解决并发注册的原子性问题。
        var scene = self.Scene;
        using (var @lock = await scene.CoroutineLockComponent.Wait((int)LockType.RegisterLock, username.GetHashCode()))
        {
            // 先查短时缓存，避免重复注册查询数据库。
            if (self.RegisterCacheDic.TryGetValue(username, out var account))
            {
                return AccountErrorCode.RegisterAccountExist;
            }

            // 缓存未命中，再查询数据库。
            var wordDatabase = scene.World.Database;
            var isExist = await wordDatabase.Exist<Account>(username);
            if (isExist)
            {
                return AccountErrorCode.RegisterAccountExist;
            }
            else // 数据库不存在，创建账号
            {
                account = Entity.Create<Account>(scene, true, true);
                account.Username = username;
                account.Password = password;
                account.CreatTime = TimeHelper.Now;
                await wordDatabase.Save(account);
                self.RegisterCacheDic.Add(username, account);
                // 注册成功后，清掉之前可能存在的“账号不存在”缓存，避免立刻登录失败
                self.LoginCacheDic.Remove(username + password);
                account.AddComponent<RegisterTimeClear>().RegisterCacheClear(4000);
                return AccountErrorCode.RegisterSuccess;
            }
        }
    }

    // 移除注册缓存，并释放缓存中的 Account 实体。
    internal static void RemoveRegisterCache(this AuthenticationComponent self, string username, bool isDispose = true)
    {
        if (!self.RegisterCacheDic.TryGetValue(username, out var account))//没有缓存
        {
            return;
        }
        if (isDispose)
        {
            self.RegisterCacheDic.Remove(username);
            account.Dispose();
        }
    }
    // 移除登录缓存，并释放缓存实体。
    internal static void RemoveLoginCache(this AuthenticationComponent self, string username, bool isDispose = true)
    {
        if (!self.LoginCacheDic.TryGetValue(username, out var account))//没有缓存
        {
            return;
        }
        if (isDispose)
        {
            self.LoginCacheDic.Remove(username);
            account.Dispose();
        }
    }
}
