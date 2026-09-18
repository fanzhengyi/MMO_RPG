using Fantasy;

internal static class AuthenticationJwtHelper
{
    // 统一从场景中获取 JWT 组件并生成 Token。
    public static string GenerateToken(Scene scene, string addressable)
    {
        return scene.GetComponent<AuthenticationJwtComponent>().GenerateJwtTokens(addressable);
    }
}
