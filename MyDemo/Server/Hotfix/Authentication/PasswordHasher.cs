using System.Security.Cryptography;
using System.Text;

/// <summary>PBKDF2 密码哈希；正确登录时升级旧明文数据。</summary>
public static class PasswordHasher
{
    private const string Prefix = "pbkdf2-sha256";
    private const int Iterations = 600000;
    private static readonly SemaphoreSlim Workers = new(Math.Clamp(Environment.ProcessorCount - 1, 1, 4));

    /// <summary>后台计算密码哈希，最多四个任务并行，给网络处理保留 CPU。</summary>
    public static Task<string> HashAsync(string password) => ComputeAsync(() => Hash(password));

    /// <summary>后台验证密码，多个鉴权场景共用计算并发上限。</summary>
    public static Task<bool> VerifyAsync(string password, string stored) => ComputeAsync(() => Verify(password, stored));

    /// <summary>等待计算名额时不阻塞场景线程，完成后归还名额。</summary>
    private static async Task<T> ComputeAsync<T>(Func<T> calculate)
    {
        await Workers.WaitAsync().ConfigureAwait(false);
        try { return await Task.Run(calculate).ConfigureAwait(false); }
        finally { Workers.Release(); }
    }

    /// <summary>每个密码使用独立随机盐，保存算法、次数、盐和结果。</summary>
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return string.Join("$", Prefix, Iterations.ToString(), Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    /// <summary>识别哈希格式，决定旧明文密码是否需要升级。</summary>
    public static bool IsHashed(string stored) => stored.StartsWith(Prefix + "$", StringComparison.Ordinal);

    /// <summary>恒定时间比较结果；兼容明文只用于迁移旧数据。</summary>
    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrEmpty(stored)) return false;
        if (!IsHashed(stored))
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(stored));
        var parts = stored.Split('$');
        if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations)
            || iterations < 100000 || iterations > 1000000) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            if (salt.Length != 16 || expected.Length != 32) return false;
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }
}
