using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Fantasy.Entitas.Interface;
using Microsoft.IdentityModel.Tokens;

public sealed class AuthenticationJwtAwakeSystem : AwakeSystem<AuthenticationJwtComponent>
{
    /// <summary>加载私钥，创建签名配置。</summary>
    protected override void Awake(AuthenticationJwtComponent self)
    {
        self.Rsa = RSA.Create();
        self.Rsa.ImportRSAPrivateKey(JwtKeyStore.ReadPrivateKey(), out _);
        self.SigningCredentials = new SigningCredentials(new RsaSecurityKey(self.Rsa), SecurityAlgorithms.RsaSha256);
    }
}

public sealed class AuthenticationJwtDestroySystem : DestroySystem<AuthenticationJwtComponent>
{
    /// <summary>释放 RSA 并清除签名引用。</summary>
    protected override void Destroy(AuthenticationJwtComponent self)
    {
        self.Rsa?.Dispose();
        self.Rsa = null!;
        self.SigningCredentials = null!;
    }
}

public static class AuthenticationJwtComponentSystem
{
    /// <summary>Token 只用于登录 Gate，五分钟后失效。</summary>
    public static string GenerateJwtTokens(this AuthenticationJwtComponent self, string userName, long gateSceneId)
    {
        // token：包含账号、目标 Gate 和有效期等声明的待签发 JWT 对象。
        var token = new JwtSecurityToken(issuer: "Fzy", audience: "Fzy",
            claims: new[] { new Claim("UserName", userName),
                new Claim("GateSceneId", gateSceneId.ToString(System.Globalization.CultureInfo.InvariantCulture)) },
            notBefore: DateTime.UtcNow, expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: self.SigningCredentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
