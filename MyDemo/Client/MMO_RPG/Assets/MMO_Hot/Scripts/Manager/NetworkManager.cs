using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;

/// <summary>只管理连接、心跳和断线事件，不处理角色或界面数据。</summary>
public sealed class NetworkManager : Singleton<NetworkManager>
{
    /// <summary>通过网关鉴权后的游戏长连接；登录、创建角色、进入世界和移动都使用它。</summary>
    public Session GateSession { get; private set; }
    // 换电脑后把这里设为 KCP；登录、同步协议和 Handler 无需改动。
    /// <summary>建立连接使用的传输协议；客户端和服务端监听配置必须匹配。</summary>
    public NetworkProtocolType Protocol { get; set; } = NetworkProtocolType.TCP;
    /// <summary>当前 Gate 长连接断开时触发，GlobalManager 负责清角色、停控制并返回登录。</summary>
    public event Action GateDisconnected;
    /// <summary>是否正在建立连接，防止并发创建连接；不等于已登录。</summary>
    private bool connecting;
    /// <summary>当前 Gate 连接的客户端上行移动序号；切场景不重置，新 Gate 连接才重置。</summary>
    private uint movementSequence;
    private uint actionSequence; // 当前 Gate 连接独立的动作命令序号，与移动采样序号分开。
    /// <summary>Fantasy 网络运行场景，不是 Unity 的 GameScene；负责创建和调度网络连接。</summary>
    private Scene scene;

    /// <summary>由启动层注入 Fantasy 场景，连接层不依赖全局 UI 管理器。</summary>
    public void Initialize(Scene networkScene)
    {
        if (networkScene == null || networkScene.IsDisposed) throw new ArgumentException("网络场景已失效。", nameof(networkScene));
        scene = networkScene;
    }

    public void Shutdown()
    {
        DisconnectGate();
        scene = null;
    }

    /// <summary>上行序号属于 Gate 连接，切场景或停用同步脚本不能重置。</summary>
    public uint NextMovementSequence() => movementSequence = unchecked(movementSequence + 1);
    public uint NextActionSequence() => actionSequence = unchecked(actionSequence + 1);

    /// <summary>跨 await 校验连接时同时比较运行时 ID，避免 Session 对象池复用。</summary>
    public bool IsCurrentGate(Session session, long runtimeId)
        => session != null && !session.IsDisposed && GateSession == session && session.RuntimeId == runtimeId;

    /// <summary>创建外网连接；当前 Fantasy Scene 同时只建立一条连接。</summary>
    public async FTask<Session> ConnectAsync(string address)
    {
        if (scene == null || scene.IsDisposed)
            throw new InvalidOperationException("Fantasy 尚未初始化完成。");
        if (connecting || (GateSession != null && !GateSession.IsDisposed))
            throw new InvalidOperationException("已有连接或连接请求正在进行。");

        connecting = true;
        // task：将连接回调转换为可 await 等待的异步任务。
        var task = FTask<Session>.Create(false);
        // session：本次操作使用的客户端网络连接。
        Session session = null;
        // completed：连接等待是否已结束，防止成功、失败和断开回调重复完成同一个任务。
        var completed = false;
        try
        {
            // 后面三个回调依次处理连接成功、连接失败、连接断开；completed 确保只结束一次等待任务。
            session = scene.Connect(address, Protocol,
                () =>
                {
                    if (completed) return;
                    completed = true;
                    task.SetResult(session);
                },
                () =>
                {
                    if (completed) return;
                    completed = true;
                    task.SetException(new InvalidOperationException($"连接失败：{address}"));
                },
                () =>
                {
                    if (!completed)
                    {
                        completed = true;
                        task.SetException(new InvalidOperationException($"连接建立前断开：{address}"));
                    }
                    OnDisconnected(session);
                },
                false, 5000);
            return await task;
        }
        catch
        {
            if (session != null && !session.IsDisposed) session.Dispose();
            throw;
        }
        finally { connecting = false; }
    }

    /// <summary>只有通过 Gate 验证的长期连接才启动心跳。</summary>
    public void SetGateSession(Session session)
    {
        if (session == null || session.IsDisposed) throw new ArgumentException("Gate 连接已失效。", nameof(session));
        GateSession = session;
        movementSequence = 0;
        actionSequence = 0;
        if (session.GetComponent<SessionHeartbeatComponent>() == null)
            session.AddComponent<SessionHeartbeatComponent>().Start(2000);
    }

    /// <summary>主动断开也触发相同的生命周期事件，业务层据此清理状态。</summary>
    public void DisconnectGate()
    {
        // session：本次操作使用的客户端网络连接。
        var session = GateSession;
        if (session == null) return;
        GateSession = null;
        if (!session.IsDisposed) session.Dispose();
        GateDisconnected?.Invoke();
    }

    /// <summary>只报告 Gate 长连接断开，鉴权短连接的关闭不触发业务事件。</summary>
    private void OnDisconnected(Session session)
    {
        // Authentication 的正常短连接关闭不应触发游戏下线。
        if (session == null || GateSession != session) return;
        GateSession = null;
        GateDisconnected?.Invoke();
    }
}
