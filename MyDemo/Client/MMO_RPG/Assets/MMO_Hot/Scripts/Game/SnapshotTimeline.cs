using System;
using System.Collections.Generic;

/// <summary>有界快照时间线；只处理时间，不依赖 Unity、网络协议或角色状态。</summary>
public sealed class SnapshotTimeline<T>
{
    /// <summary>一个按相对服务端时间排列的样本。</summary>
    private struct Entry
    {
        /// <summary>距首个服务端样本的时间，单位秒；不是本机收包时间。</summary>
        public double Time;
        /// <summary>该时刻的数据，例如角色位置、朝向和移动状态。</summary>
        public T Value;
    }
    /// <summary>按服务端时间递增保存的样本；旧样本会逐步移除，避免无限占用内存。</summary>
    private readonly List<Entry> entries = new List<Entry>(32);
    /// <summary>故意延后的播放时间，单位秒；用少量显示延迟换取收包抖动下的平滑移动。</summary>
    private readonly double interpolationDelay;
    /// <summary>最多保留多少个样本，至少需要两个才能插值。</summary>
    private readonly int capacity;
    /// <summary>首个样本的服务端毫秒时间，后续时间减去它再换算为秒。</summary>
    private long serverOrigin;
    /// <summary>首个样本抵达本机的时间，单位秒，与服务端时钟分别建立原点。</summary>
    private double localOrigin;
    /// <summary>根据较早抵达样本估算的相对时钟偏移，单位秒；不代表精确的网络往返延迟。</summary>
    private double clockOffset;
    /// <summary>上一次播放到的相对服务端秒数，防止播放时钟倒退。</summary>
    private double lastPlaybackTime;
    /// <summary>当前保留的样本数量。</summary>
    public int Count => entries.Count;

    // interpolationDelay 单位为秒，默认 150 毫秒；capacity 是队列长度，不是帧率。
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
    // serverTimeMs 是服务端毫秒值，receivedAt 是本机不受暂停影响的秒值，value 是复制后的业务数据。
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
        // time：本样本相对首个服务端样本的时间，单位秒。
        var time = (serverTimeMs - serverOrigin) / 1000d;
        // last：当前最后一个样本的数组下标，空队列时为 -1。
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
    // now 使用与 receivedAt 相同的本机时钟；from/to 是两端样本，fraction 是 0 到 1 的插值比例。
    public bool TrySample(double now, out T from, out T to, out float fraction)
    {
        from = to = default;
        fraction = 0;
        if (entries.Count == 0) return false;
        // playback：延后插值且保证不倒退的播放时刻，单位为相对服务端秒数。
        var playback = Math.Max(lastPlaybackTime, now - localOrigin - clockOffset - interpolationDelay);
        lastPlaybackTime = playback;
        while (entries.Count > 2 && entries[1].Time <= playback) entries.RemoveAt(0);
        // first：插值区间的较早样本。
        var first = entries[0];
        // second：插值区间的较晚样本，没有第二个时复用第一个。
        var second = entries.Count > 1 ? entries[1] : first;
        from = first.Value;
        to = second.Value;
        // duration：两个样本之间的服务端时间差，单位秒。
        var duration = second.Time - first.Time;
        fraction = duration <= 0 ? 1f : (float)Math.Max(0, Math.Min(1, (playback - first.Time) / duration));
        return true;
    }
}
