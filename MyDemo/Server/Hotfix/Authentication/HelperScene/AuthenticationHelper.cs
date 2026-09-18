using Fantasy;
using Fantasy.Async;

public static class AuthenticationHelper
{
    // Helper 只负责转发调用，避免 Handler 直接依赖组件查找细节。
    public static async FTask<AccountErrorCode> Resgister(Scene scene, string account, string password)
    {
        return await scene.GetComponent<AuthenticationComponent>().Resgister(account, password);
    }

    public static async FTask<uint> Login(Scene scene, string account, string password)
    {
        return await scene.GetComponent<AuthenticationComponent>().Login(account, password);
    }

    internal static void RemoveRegisterCache(Scene scene, string username, bool isDispose)
    {
        scene.GetComponent<AuthenticationComponent>().RemoveRegisterCache(username, isDispose);
    }

    internal static void RemoveLoginCache(Scene scene, string username, bool isDispose)
    {
        scene.GetComponent<AuthenticationComponent>().RemoveLoginCache(username, isDispose);
    }
}
