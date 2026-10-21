using Fantasy;
using Fantasy.Async;

/// <summary>初始化框架，并衔接连接生命周期与游戏界面。</summary>
public sealed class GlobalManager : MonoSingleton<GlobalManager>
{
    /// <summary>Fantasy 的主线程网络场景，供 NetworkManager 建立连接；与 Unity 场景不是同一种对象。</summary>
    public Scene Scene { get; private set; }

    private void Start() => InitializeAsync().Coroutine();

    /// <summary>框架准备完成后再显示登录界面，避免初始化期间点击连接。</summary>
    private async FTask InitializeAsync()
    {
        await Fantasy.Platform.Unity.Entry.Initialize();
        if (this == null || Instance != this) return;
        // scene：刚创建的 Fantasy 主线程网络场景，用来初始化连接管理器。
        var scene = await Scene.Create(SceneRuntimeMode.MainThread);
        if (this == null || Instance != this)
        {
            scene.Dispose();
            return;
        }
        Scene = scene;
        NetworkManager.Instance.Initialize(scene);
        NetworkManager.Instance.GateDisconnected += OnGateDisconnected;
        UIManager.Instance.ShowPanel<LoginPanel>();
    }

    /// <summary>断线后清空数据和远端模型，再回到登录入口。</summary>
    private void OnGateDisconnected()
    {
        GameWorldView.Suspend();
        PlayerSelfModel.Instance.Clear();
        RemotePlayerManager.Reset();
        UIManager.Instance.Hide<ChooseRolePanel>();
        UIManager.Instance.ShowPanel<LoginPanel>();
    }

    /// <summary>释放连接、场景以及事件订阅。</summary>
    protected override void OnDestroy()
    {
        if (Instance == this)
        {
            NetworkManager.Instance.GateDisconnected -= OnGateDisconnected;
            NetworkManager.Instance.Shutdown();
            Scene?.Dispose();
        }
        base.OnDestroy();
    }
}
