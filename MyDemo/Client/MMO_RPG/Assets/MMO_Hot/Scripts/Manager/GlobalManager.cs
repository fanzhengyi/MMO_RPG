using Fantasy;
using Fantasy.Async;

/// <summary>
/// 客户端运行入口。这里只负责初始化第三方框架和打开第一个界面。
/// 网络连接统一交给 NetworkManager。
/// </summary>
public class GlobalManager : MonoSingleton<GlobalManager>
{
    public Scene Scene { get; private set; }

    private void Start()
    {
        UIManager.Instance.Show<LoginPanel>();
        InitializeAsync().Coroutine();
    }

    private async FTask InitializeAsync()
    {
        await Fantasy.Platform.Unity.Entry.Initialize();
        Scene = await Scene.Create(SceneRuntimeMode.MainThread);
    }
}
