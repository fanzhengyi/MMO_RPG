using System;
using System.Collections.Generic;
using System.Linq;
using Fantasy.Helper;

/// <summary>
/// LoginServer 地址配置。
/// 使用一致性哈希环按账号稳定分配登录服；增减服务器时，只有少量账号会重新分配。
/// </summary>
public static class LoginServerConfig
{
    private static readonly List<string> LoginServers =new List<string>
    {
         "127.0.0.1:20001",
    "127.0.0.1:20002",
    "127.0.0.1:20003",
    };


    public static string GetAddress(string account)
    {
        uint userHash = HashCodeHelper.MurmurHash3(account);
        int index = (int)(userHash % LoginServers.Count);

        return LoginServers[index];
    }
}
