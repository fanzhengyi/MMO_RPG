using System.Security.Cryptography;
using Fantasy.Entitas;
using Microsoft.IdentityModel.Tokens;

/// <summary>Gate 只持有验签公钥。</summary>
public sealed class GateJwtComponent : Entity
{
    public RSA Rsa = null!;
    public TokenValidationParameters TokenValidationParameters = null!;
}

