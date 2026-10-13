using Fantasy.Helper;

/// <summary>按账号哈希取模选择鉴权服；顺序与服务端场景配置保持一致。</summary>
public static class LoginServerConfig
{
    private static readonly string[] Addresses =
    {
        "127.0.0.1:20001",
        "127.0.0.1:20002",
        "127.0.0.1:20003",
    };

    public static string GetAddress(string account)
        => Addresses[(int)(HashCodeHelper.MurmurHash3(account) % Addresses.Length)];
}
