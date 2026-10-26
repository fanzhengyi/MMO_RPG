using System.Security.Cryptography;
using System.Text;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
using Fantasy.Platform.Net;

/// <summary>清空 Authentication 的短时缓存。</summary>
public sealed class AuthenticationComponentDestroySystem : DestroySystem<AuthenticationComponent>
{
    protected override void Destroy(AuthenticationComponent self)
    {
        // account：从数据库或缓存取得的账号实体，不是角色实体。
        foreach (var account in self.RegisterCacheDic.Values) account.Dispose();
        // cache：当前被清理的登录结果缓存实体。
        foreach (var cache in self.LoginCacheDic.Values) cache.Dispose();
        self.RegisterCacheDic.Clear();
        self.LoginCacheDic.Clear();
    }
}

/// <summary>处理账号验证和注册；数据库访问只发生在鉴权服务中。</summary>
public static class AuthenticationComponentSystem
{
    /// <summary>缓存当前鉴权场景的路由索引。</summary>
    public static void UpdateServerData(this AuthenticationComponent self)
    {
        // servers：配置中全部鉴权场景，顺序决定账号哈希分片。
        var servers = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Authentication);
        self.LocalServerConfigIndex = servers.IndexOf(SceneConfigData.Instance.Get(self.Scene.SceneConfigId));
        self.AuthernServerCount = servers.Count;
    }

    /// <summary>校验密码，短时缓存结果；正确的旧密码在登录时升级为哈希。</summary>
    public static async FTask<ResultCode> Login(this AuthenticationComponent self, string accountUser, string password)
    {
        // error：账号输入和鉴权分片的验证结果。
        var error = Validate(self, accountUser, password);
        if (error != ResultCode.Success) return error;
        // scene：当前操作所属的 Fantasy 网络场景，负责消息、实体和定时器。
        var scene = self.Scene;
        using (await scene.CoroutineLockComponent.Wait((int)LockType.Account, accountUser.GetHashCode()))
        {
            // key：账号与密码组合的哈希缓存键，不存明文密码。
            var key = GetCacheKey(accountUser, password);
            // cached：已缓存的登录验证结果。
            if (self.LoginCacheDic.TryGetValue(key, out var cached)) return cached.Result;
            // account：从数据库或缓存取得的账号实体，不是角色实体。
            using var account = await scene.World.Database.First<Account>(d => d.AccountUser == accountUser);
            // result：本次密码验证的业务结果，默认按凭证错误处理。
            var result = ResultCode.IncorrectCredentials;
            if (account != null)
            {
                // 密码计算放到工作线程，避免阻塞同一场景的其他网络消息。
                // valid：本次输入密码是否与账号存储密码匹配。
                var valid = await PasswordHasher.VerifyAsync(password, account.Password);
                if (valid)
                {
                    if (!PasswordHasher.IsHashed(account.Password))
                        account.Password = await PasswordHasher.HashAsync(password);
                    account.LoginTime = TimeHelper.Now;
                    await scene.World.Database.Save(account);
                    result = ResultCode.Success;
                }
            }
            // entry：新建的登录结果缓存实体，短时间后自动清理。
            var entry = Entity.Create<LoginCacheEntity>(scene, true, true);
            entry.Result = result;
            self.LoginCacheDic[key] = entry;
            entry.AddComponent<LoginTimeClear>().TimeOut(key, 4000);
            return result;
        }
    }

    /// <summary>在同一账号锁内检查存在性并创建账号。</summary>
    public static async FTask<ResultCode> Register(this AuthenticationComponent self, string accountUser, string password)
    {
        // error：账号输入和鉴权分片的验证结果。
        var error = Validate(self, accountUser, password);
        if (error != ResultCode.Success) return error;
        // scene：当前操作所属的 Fantasy 网络场景，负责消息、实体和定时器。
        var scene = self.Scene;
        using (await scene.CoroutineLockComponent.Wait((int)LockType.Account, accountUser.GetHashCode()))
        {
            if (self.RegisterCacheDic.ContainsKey(accountUser)
                || await scene.World.Database.First<Account>(d => d.AccountUser == accountUser) != null)
                return ResultCode.AccountExists;

            // account：从数据库或缓存取得的账号实体，不是角色实体。
            var account = Entity.Create<Account>(scene, true, true);
            account.AccountUser = accountUser;
            account.Password = await PasswordHasher.HashAsync(password);
            account.CreatTime = TimeHelper.Now;
            try
            {
                await scene.World.Database.Save(account);
                self.RegisterCacheDic[accountUser] = account;
                account.AddComponent<RegisterTimeClear>().RegisterCacheClear(4000);
            }
            catch
            {
                account.Dispose();
                throw;
            }
            self.RemoveLoginCache(GetCacheKey(accountUser, password));
            return ResultCode.Success;
        }
    }

    /// <summary>限制输入长度，并确认账号所属的鉴权场景。</summary>
    private static ResultCode Validate(AuthenticationComponent self, string accountUser, string password)
    {
        if (string.IsNullOrWhiteSpace(accountUser) || accountUser.Length > 64
            || string.IsNullOrEmpty(password) || password.Length > 128)
            return ResultCode.InvalidCredentials;
        if (self.AuthernServerCount == 0
            || self.LocalServerConfigIndex != HashCodeHelper.MurmurHash3(accountUser) % self.AuthernServerCount)
            return ResultCode.WrongAuthenticationServer;
        return ResultCode.Success;
    }

    /// <summary>长度前缀防止拼接碰撞；缓存键不保存明文凭证。</summary>
    private static string GetCacheKey(string accountUser, string password)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{accountUser.Length}:{accountUser}{password}")));

    /// <summary>移除注册缓存并释放账号实体。</summary>
    internal static void RemoveRegisterCache(this AuthenticationComponent self, string accountUser)
    {
        // account：从数据库或缓存取得的账号实体，不是角色实体。
        if (self.RegisterCacheDic.Remove(accountUser, out var account)) account.Dispose();
    }

    /// <summary>移除登录结果并取消其过期任务。</summary>
    internal static void RemoveLoginCache(this AuthenticationComponent self, string key)
    {
        // cache：当前被清理的登录结果缓存实体。
        if (self.LoginCacheDic.Remove(key, out var cache)) cache.Dispose();
    }
}
