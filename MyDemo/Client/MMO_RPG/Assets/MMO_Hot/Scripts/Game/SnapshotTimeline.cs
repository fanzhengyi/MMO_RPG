using System;
using System.Collections.Generic;

/// <summary>有界快照时间线；只处理时间，不依赖 Unity、网络协议或角色状态。</summary>
public sealed class SnapshotTimeline<T>
{
    private struct Entry { public double Time; public T Value; }
    private readonly List<Entry> entries = new List<Entry>(32);
    private readonly double interpolationDelay;
    private readonly int capacity;
    private long serverOrigin;
    private double localOrigin;
    private double clockOffset;
    private double lastPlaybackTime;
    public int Count => entries.Count;

    public SnapshotTimeline(double interpolationDelay = 0.15, int capacity = 32)
    {
        if (interpolationDelay < 0 || double.IsNaN(interpolationDelay) || double.IsInfinity(interpolationDelay))
            throw new ArgumentOutOfRangeException(nameof(interpolationDelay));
        if (capacity < 2) throw new ArgumentOutOfRangeException(nameof(capacity));
        this.interpolationDelay = interpolationDelay;
        this.capacity = capacity;
    }

    public void Clear()
    {
        entries.Clear();
        lastPlaybackTime = double.NegativeInfinity;
    }

    /// <summary>用服务端单调时钟排序；相对时间避免长时间运行后的浮点精度损失。</summary>
    public bool Add(long serverTimeMs, double receivedAt, T value)
    {
        if (double.IsNaN(receivedAt) || double.IsInfinity(receivedAt)) return false;
        if (entries.Count == 0)
        {
            serverOrigin = serverTimeMs;
            localOrigin = receivedAt;
            clockOffset = 0;
            lastPlaybackTime = double.NegativeInfinity;
        }
        var time = (serverTimeMs - serverOrigin) / 1000d;
        var last = entries.Count - 1;
        if (last >= 0 && time < entries[last].Time) return false;
        // 同毫秒内的状态切换保留最新版本，避免除以零。
        if (last >= 0 && time == entries[last].Time) entries[last] = new Entry { Time = time, Value = value };
        else entries.Add(new Entry { Time = time, Value = value });
        // 早到样本估计时钟偏移；延迟突发不会把播放时钟拉回去。
        clockOffset = Math.Min(clockOffset, receivedAt - localOrigin - time);
        if (entries.Count > capacity) entries.RemoveAt(0);
        return true;
    }

    /// <summary>返回包围播放时刻的两个样本；没有未来样本时停在最后确认位置。</summary>
    public bool TrySample(double now, out T from, out T to, out float fraction)
    {
        from = to = default;
        fraction = 0;
        if (entries.Count == 0) return false;
        var playback = Math.Max(lastPlaybackTime, now - localOrigin - clockOffset - interpolationDelay);
        lastPlaybackTime = playback;
        while (entries.Count > 2 && entries[1].Time <= playback) entries.RemoveAt(0);
        var first = entries[0];
        var second = entries.Count > 1 ? entries[1] : first;
        from = first.Value;
        to = second.Value;
        var duration = second.Time - first.Time;
        fraction = duration <= 0 ? 1f : (float)Math.Max(0, Math.Min(1, (playback - first.Time) / duration));
        return true;
    }
}
