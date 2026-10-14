using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;

/// <summary>只管理连接、心跳和断线事件，不处理角色或界面数据。</summary>
public sealed class NetworkManager : Singleton<NetworkManager>
{
    public Session GateSession { get; private set; }
    // 换电脑后把这里设为 KCP；登录、同步协议和 Handler 无需改动。
    public NetworkProtocolType Protocol { get; set; } = NetworkProtocolType.TCP;
    public event Action GateDisconnected;
    private bool connecting;
    private uint movementSequence;

    /// <summary>上行序号属于 Gate 连接，切场景或停用同步脚本不能重置。</summary>
    public uint NextMovementSequence() => movementSequence = unchecked(movementSequence + 1);

    /// <summary>跨 await 校验连接时同时比较运行时 ID，避免 Session 对象池复用。</summary>
    public bool IsCurrentGate(Session session, long runtimeId)
        => session != null && !session.IsDisposed && GateSession == session && session.RuntimeId == runtimeId;

    /// <summary>创建外网连接；当前 Fantasy Scene 同时只建立一条连接。</summary>
    public async FTask<Session> ConnectAsync(string address)
    {
        if (GlobalManager.Instance.Scene == null)
            throw new InvalidOperationException("Fantasy 尚未初始化完成。");
        if (connecting || (GateSession != null && !GateSession.IsDisposed))
            throw new InvalidOperationException("已有连接或连接请求正在进行。");

        connecting = true;
        var task = FTask<Session>.Create(false);
        Session session = null;
        var completed = false;
        try
        {
            session = GlobalManager.Instance.Scene.Connect(address, Protocol,
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
        if (session.GetComponent<SessionHeartbeatComponent>() == null)
            session.AddComponent<SessionHeartbeatComponent>().Start(2000);
    }

    /// <summary>主动断开也触发相同的生命周期事件，业务层据此清理状态。</summary>
    public void DisconnectGate()
    {
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
