using Fantasy.Entitas.Interface;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

public sealed class GateJwtComponentAwakeSystem : AwakeSystem<GateJwtComponent>
{
    protected override void Awake(GateJwtComponent self)
    {
        self.Awake();
    }
}

public static class GateJwtComponentSystem
{
    public static void Awake(this GateJwtComponent self)
    {
        // Gate 使用公钥验证 Authentication 签发的 Token。
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(self.PublicKeyPem), out _);
        self.SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
        self.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "Fzy",
            ValidAudience = "Fzy",
            IssuerSigningKey = new RsaSecurityKey(rsa),
        };
    }

    public static bool ValidateJwtTokens(this GateJwtComponent self, string token)
    {
        // 验证失败时拒绝当前连接，避免伪造 Token 进入 Gate。
        try
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            jwtSecurityTokenHandler.ValidateToken(token, self.TokenValidationParameters, out _);
            return true;
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            Console.WriteLine("验证受众失败");
            return false;
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            Console.WriteLine("发行验证失败");
            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}
