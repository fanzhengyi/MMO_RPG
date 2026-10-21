using System.Security.Cryptography;
using Fantasy.Entitas;
using Microsoft.IdentityModel.Tokens;

/// <summary>Authentication 持有签名私钥；RSA 随场景生命周期释放。</summary>
public sealed class AuthenticationJwtComponent : Entity
{
    /// <summary>鉴权服务持有的 RSA 私钥对象，用于签发 Token，随场景销毁释放。</summary>
    public RSA Rsa = null!;
    /// <summary>JWT 签名所用的密钥和算法配置。</summary>
    public SigningCredentials SigningCredentials = null!;
}
