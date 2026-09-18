using Fantasy;

public static class GateJwtHelper
{
    // 统一从 Gate 场景中获取 JWT 组件进行验证。
    public static bool ValidateToken(Scene scene, string token)
    {
        return scene.GetComponent<GateJwtComponent>().ValidateJwtTokens(token);
    }
}
