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
        foreach (var account in self.RegisterCacheDic.Values) account.Dispose();
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
        var servers = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Authentication);
        self.LocalServerConfigIndex = servers.IndexOf(SceneConfigData.Instance.Get(self.Scene.SceneConfigId));
        self.AuthernServerCount = servers.Count;
    }

    /// <summary>校验密码，短时缓存结果；正确的旧密码在登录时升级为哈希。</summary>
    public static async FTask<ResultCode> Login(this AuthenticationComponent self, string username, string password)
    {
        var error = Validate(self, username, password);
        if (error != ResultCode.Success) return error;
        var scene = self.Scene;
        using (await scene.CoroutineLockComponent.Wait((int)LockType.Account, username.GetHashCode()))
        {
            var key = GetCacheKey(username, password);
            if (self.LoginCacheDic.TryGetValue(key, out var cached)) return cached.Result;
            using var account = await scene.World.Database.First<Account>(d => d.Username == username);
            var result = ResultCode.IncorrectCredentials;
            if (account != null)
            {
                // 密码计算放到工作线程，避免阻塞同一场景的其他网络消息。
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
            var entry = Entity.Create<LoginCacheEntity>(scene, true, true);
            entry.Result = result;
            self.LoginCacheDic[key] = entry;
            entry.AddComponent<LoginTimeClear>().TimeOut(key, 4000);
            return result;
        }
    }

    /// <summary>在同一账号锁内检查存在性并创建账号。</summary>
    public static async FTask<ResultCode> Register(this AuthenticationComponent self, string username, string password)
    {
        var error = Validate(self, username, password);
        if (error != ResultCode.Success) return error;
        var scene = self.Scene;
        using (await scene.CoroutineLockComponent.Wait((int)LockType.Account, username.GetHashCode()))
        {
            if (self.RegisterCacheDic.ContainsKey(username)
                || await scene.World.Database.First<Account>(d => d.Username == username) != null)
                return ResultCode.AccountExists;

            var account = Entity.Create<Account>(scene, true, true);
            account.Username = username;
            account.Password = await PasswordHasher.HashAsync(password);
            account.CreatTime = TimeHelper.Now;
            try
            {
                await scene.World.Database.Save(account);
                self.RegisterCacheDic[username] = account;
                account.AddComponent<RegisterTimeClear>().RegisterCacheClear(4000);
            }
            catch
            {
                account.Dispose();
                throw;
            }
            self.RemoveLoginCache(GetCacheKey(username, password));
            return ResultCode.Success;
        }
    }

    /// <summary>限制输入长度，并确认账号所属的鉴权场景。</summary>
    private static ResultCode Validate(AuthenticationComponent self, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length > 64
            || string.IsNullOrEmpty(password) || password.Length > 128)
            return ResultCode.InvalidCredentials;
        if (self.AuthernServerCount == 0
            || self.LocalServerConfigIndex != HashCodeHelper.MurmurHash3(username) % self.AuthernServerCount)
            return ResultCode.WrongAuthenticationServer;
        return ResultCode.Success;
    }

    /// <summary>长度前缀防止拼接碰撞；缓存键不保存明文凭证。</summary>
    private static string GetCacheKey(string username, string password)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{username.Length}:{username}{password}")));

    /// <summary>移除注册缓存并释放账号实体。</summary>
    internal static void RemoveRegisterCache(this AuthenticationComponent self, string username)
    {
        if (self.RegisterCacheDic.Remove(username, out var account)) account.Dispose();
    }

    /// <summary>移除登录结果并取消其过期任务。</summary>
    internal static void RemoveLoginCache(this AuthenticationComponent self, string key)
    {
        if (self.LoginCacheDic.Remove(key, out var cache)) cache.Dispose();
    }
}
