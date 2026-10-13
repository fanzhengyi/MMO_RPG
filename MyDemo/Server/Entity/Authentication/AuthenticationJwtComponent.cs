using System.Security.Cryptography;
using Fantasy.Entitas;
using Microsoft.IdentityModel.Tokens;

/// <summary>Authentication 持有签名私钥；RSA 随场景生命周期释放。</summary>
public sealed class AuthenticationJwtComponent : Entity
{
    public RSA Rsa = null!;
    public SigningCredentials SigningCredentials = null!;
}
