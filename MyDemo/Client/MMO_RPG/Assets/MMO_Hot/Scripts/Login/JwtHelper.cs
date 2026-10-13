using System;
using System.Text;
using Fantasy.Helper;

/// <summary>客户端只读取 Token 内的 Gate 地址，验签由服务端完成。</summary>
public sealed class JwtPayload
{
    public string Address { get; set; }
}
public static class JwtHelper
{
    /// <summary>解析 JWT 的 Base64Url 数据段，失败返回 false。</summary>
    public static bool TryGetPayload(string token, out JwtPayload payload)
    {
        payload = null;

        try
        {
            // JWT 由 Header.Payload.Signature 三段组成，这里只读取 Payload 中的 Gate 地址。
            string[] parts = token.Split('.');
            if (parts.Length != 3)
                return false;

            string base64Payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (base64Payload.Length % 4)
            {
                case 2: base64Payload += "=="; break;
                case 3: base64Payload += "="; break;
            }

            string json = Encoding.UTF8.GetString(Convert.FromBase64String(base64Payload));
            payload = JsonHelper.Deserialize<JwtPayload>(json);
            return payload != null && !string.IsNullOrEmpty(payload.Address);
        }
        catch
        {
            payload = null;
            return false;
        }
    }
}
