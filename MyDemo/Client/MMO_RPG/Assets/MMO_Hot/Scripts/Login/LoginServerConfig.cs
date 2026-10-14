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

    /// <summary>根据账号选择与服务端配置对应的鉴权地址。</summary>
    public static string GetAddress(string account)
        => Addresses[(int)(HashCodeHelper.MurmurHash3(account) % Addresses.Length)];
}
