using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Fantasy.Entitas.Interface;
using Microsoft.IdentityModel.Tokens;

public sealed class GateJwtAwakeSystem : AwakeSystem<GateJwtComponent>
{
    /// <summary>加载公钥，配置签名、算法和有效期校验。</summary>
    protected override void Awake(GateJwtComponent self)
    {
        self.Rsa = RSA.Create();
        self.Rsa.ImportRSAPublicKey(JwtKeyStore.ReadPublicKey(), out _);
        self.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
            RequireExpirationTime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = "Fzy", ValidAudience = "Fzy",
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
            ClockSkew = TimeSpan.FromSeconds(30),
            IssuerSigningKey = new RsaSecurityKey(self.Rsa),
        };
    }
}

public sealed class GateJwtDestroySystem : DestroySystem<GateJwtComponent>
{
    /// <summary>释放 RSA 并清除验签配置。</summary>
    protected override void Destroy(GateJwtComponent self)
    {
        self.Rsa?.Dispose();
        self.Rsa = null!;
        self.TokenValidationParameters = null!;
    }
}

public static class GateJwtComponentSystem
{
    /// <summary>验签后提取账号，并确认凭证属于当前 Gate。</summary>
    public static bool TryValidateToken(this GateJwtComponent self, string token, long gateSceneId, out string userName)
    {
        userName = "";
        if (string.IsNullOrWhiteSpace(token) || token.Length > 8192) return false;
        try
        {
            // principal：JWT 验证成功后得到的身份声明集合。
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, self.TokenValidationParameters, out _);
            // account：从签名凭证中读出的账号声明，不是数据库实体。
            var account = principal.FindFirst("UserName")?.Value;
            if (string.IsNullOrWhiteSpace(account) || account.Length > 64
                // targetId：Token 指定的目标 Gate 场景配置 ID。
                || !long.TryParse(principal.FindFirst("GateSceneId")?.Value, out var targetId)
                || targetId != gateSceneId) return false;
            userName = account;
            return true;
        }
        catch (SecurityTokenException) { return false; }
        catch (ArgumentException) { return false; }
    }
}
