using System.Security.Cryptography;
using System.Text;

/// <summary>RSA 密钥文件入口；多进程部署指定统一密钥目录。</summary>
public static class JwtKeyStore
{
    /// <summary>鉴权场景读取签名私钥。</summary>
    public static byte[] ReadPrivateKey() => Read("private.key");
    /// <summary>Gate 场景读取验签公钥。</summary>
    public static byte[] ReadPublicKey() => Read("public.key");

    /// <summary>部署环境只读取文件，本机开发缺少密钥时自动生成。</summary>
    private static byte[] Read(string name)
    {
        // configured：环境变量指定的密钥目录，未配置时为 null。
        var configured = Environment.GetEnvironmentVariable("FANTASY_JWT_KEY_DIRECTORY");
        // directory：本次实际使用的 RSA 密钥文件目录。
        var directory = configured ?? Path.Combine(AppContext.BaseDirectory, "JwtKeys");
        if (configured != null) return File.ReadAllBytes(Path.Combine(directory, name));

        // 鉴权和 Gate 可能属于不同进程，进程内 lock 无法防止同时生成两套密钥。
        // directoryHash：密钥目录路径的哈希，用于区分不同目录对应的进程锁名称。
        var directoryHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(directory))));
        // mutex：跨进程互斥锁，避免鉴权和 Gate 同时生成两套开发密钥。
        using var mutex = new Mutex(false, "Fantasy.JwtKeys." + directoryHash);
        // acquired：是否已取得互斥锁，只有取得后才能在 finally 释放。
        var acquired = false;
        try
        {
            try { acquired = mutex.WaitOne(TimeSpan.FromSeconds(10)); }
            catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) throw new TimeoutException("等待 JWT 开发密钥生成超时。");
            if (!File.Exists(Path.Combine(directory, "public.key")))
            {
                Directory.CreateDirectory(directory);
                // rsa：首次启动时临时生成的 RSA 密钥对。
                using var rsa = RSA.Create(2048);
                File.WriteAllBytes(Path.Combine(directory, "private.key"), rsa.ExportRSAPrivateKey());
                File.WriteAllBytes(Path.Combine(directory, "public.key"), rsa.ExportRSAPublicKey());
            }
            return File.ReadAllBytes(Path.Combine(directory, name));
        }
        finally { if (acquired) mutex.ReleaseMutex(); }
    }
}
