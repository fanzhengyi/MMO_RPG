using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using UnityEngine;

public class NetworkManager : Singleton<NetworkManager>
{
    public Session GateSession { get; private set; }


    /// <summary>
    /// 建立一条外网连接。当前 Fantasy Scene 同一时间只能持有一条外网连接。
    /// </summary>
    public async FTask<Session> ConnectAsync(string address)
    {
        if (GlobalManager.Instance.Scene == null)
            throw new InvalidOperationException("Fantasy 尚未初始化完成。");

        FTask<Session> connectTask = FTask<Session>.Create(false);
        Session newSession = null;

        newSession = GlobalManager.Instance.Scene.Connect(
            address,
            NetworkProtocolType.KCP,
            () => connectTask.SetResult(newSession),
            () => connectTask.SetException(new Exception($"连接失败：{address}")),
            () => OnConnectDisconnected(newSession, address),
            false,
            5000);

        return await connectTask;
    }

    /// <summary>
    /// 将已通过 Gate 登录验证的连接设为长期游戏连接。
    /// </summary>
    public void SetGateSession(Session session)
    {
        GateSession = session;
        // 只有长期 Gate 连接需要心跳；登录服和注册连接不需要。
        if (GateSession.GetComponent<SessionHeartbeatComponent>() == null)
            GateSession.AddComponent<SessionHeartbeatComponent>().Start(2000);
    }

    public void DisconnectGate()
    {
        if (GateSession != null && !GateSession.IsDisposed)
            GateSession.Dispose();
        GateSession = null;
    }


    //连接断开
    private void OnConnectDisconnected(Session session, string address)
    {
        Log.Error($"与服务器断开连接：{address}");
        if (GateSession == session)
            GateSession = null;
    }
}
