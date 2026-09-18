using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Fantasy;
using Fantasy.Entitas.Interface;
using Microsoft.IdentityModel.Tokens;

public sealed class AuthenticationJwtComponentAwakeSystem : AwakeSystem<AuthenticationJwtComponent>
{
    protected override void Awake(AuthenticationJwtComponent self)
    {
        self.Awake();
    }
}

public static class AuthenticationJwtHelperAuthenticationJwtComponentSystem
{
    public static void Awake(this AuthenticationJwtComponent self)
    {
        // Authentication 使用私钥签发 Token；Gate 只保存公钥进行验证。
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(self.PublicKeyPem), out _);
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(self.PrivateKeyPem), out _);
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
            ClockSkew = TimeSpan.FromMinutes(5),
        };
    }

    public static string GenerateJwtTokens(this AuthenticationJwtComponent self, string Address)
    {
        // Token 中只放 Gate 地址，客户端拿到后再连接对应 Gate。
        var jwtPayload = new JwtPayload()
        {
            { "Address", Address },
        };

        var jwtSecurityToken = new JwtSecurityToken
            (issuer: "Fzy",
            audience: "Fzy",
            claims: jwtPayload.Claims,
            expires: DateTime.UtcNow.AddMicroseconds(3000),
            signingCredentials: self.SigningCredentials
            );
        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(jwtSecurityToken);
    }
}
