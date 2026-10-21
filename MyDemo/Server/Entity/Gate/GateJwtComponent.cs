using System.Security.Cryptography;
using Fantasy.Entitas;
using Microsoft.IdentityModel.Tokens;

/// <summary>Gate 只持有验签公钥。</summary>
public sealed class GateJwtComponent : Entity
{
    /// <summary>Gate 持有的 RSA 公钥对象，只能验证签名，不能签发登录凭证。</summary>
    public RSA Rsa = null!;
    /// <summary>JWT 验证规则，包括签名、签发方、接收方和有效期等条件。</summary>
    public TokenValidationParameters TokenValidationParameters = null!;
}

