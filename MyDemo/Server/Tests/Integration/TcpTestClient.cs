using System.Buffers.Binary;
using System.Net.Sockets;
using System.Threading.Channels;
using Fantasy.Network.Interface;
using Fantasy.PacketParser;
using LightProto;

/// <summary>真实 TCP + 生成的 ProtoBuf 协议；不在测试进程中直接调用业务 Handler。</summary>
internal sealed class TcpTestClient : IDisposable
{
    private readonly TcpClient socket = new() { NoDelay = true };
    private readonly Channel<Frame> received = Channel.CreateUnbounded<Frame>();
    private readonly List<Frame> pending = new();
    private uint rpcId;
    private NetworkStream Stream => socket.GetStream();
    internal readonly record struct Frame(uint Opcode, uint RpcId, byte[] Payload)
    {
        public T Read<T>() where T : IProtoParser<T> => Serializer.Deserialize<T>(Payload.AsSpan());
    }

    public static async Task<TcpTestClient> ConnectAsync(string address)
    {
        var endpoint = System.Net.IPEndPoint.Parse(address);
        if (!System.Net.IPAddress.IsLoopback(endpoint.Address)) throw new InvalidOperationException("Integration tests require a loopback endpoint.");
        var client = new TcpTestClient();
        await client.socket.ConnectAsync(endpoint.Address, endpoint.Port);
        _ = client.ReceiveAsync();
        return client;
    }

    public async Task<TResponse> CallAsync<TRequest, TResponse>(TRequest request)
        where TRequest : IMessage, IProtoParser<TRequest>
        where TResponse : IProtoParser<TResponse>
    {
        var id = ++rpcId;
        await SendAsync(request, id);
        var frame = await WaitAsync(f => f.RpcId == id, 8000);
        if (frame == null) throw new TimeoutException($"RPC timeout: {typeof(TRequest).Name}");
        return frame.Value.Read<TResponse>();
    }

    public async Task SendAsync<T>(T message, uint id = 0) where T : IMessage, IProtoParser<T>
    {
        using var buffer = new MemoryStream();
        buffer.Position = Packet.OuterPacketHeadLength;
        Serializer.Serialize(buffer, message);
        var bytes = buffer.ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(bytes, bytes.Length == Packet.OuterPacketHeadLength ? -1 : bytes.Length - Packet.OuterPacketHeadLength);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(Packet.PacketLength), message.OpCode());
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(Packet.OuterPacketRpcIdLocation), id);
        await Stream.WriteAsync(bytes);
    }

    public async Task<Frame?> WaitAsync(Func<Frame, bool> predicate, int timeoutMs = 3000)
    {
        var index = pending.FindIndex(f => predicate(f));
        if (index >= 0) { var frame = pending[index]; pending.RemoveAt(index); return frame; }
        using var cancellation = new CancellationTokenSource(timeoutMs);
        try
        {
            while (await received.Reader.WaitToReadAsync(cancellation.Token))
            while (received.Reader.TryRead(out var frame))
            {
                if (predicate(frame)) return frame;
                pending.Add(frame);
            }
        }
        catch (OperationCanceledException) { }
        return null;
    }

    private async Task ReceiveAsync()
    {
        try
        {
            var header = new byte[Packet.OuterPacketHeadLength];
            while (true)
            {
                await Stream.ReadExactlyAsync(header);
                var length = BinaryPrimitives.ReadInt32LittleEndian(header);
                if (length < -1 || length > 1048560) throw new InvalidDataException("Invalid test response length.");
                var payload = new byte[Math.Max(0, length)];
                await Stream.ReadExactlyAsync(payload);
                received.Writer.TryWrite(new Frame(BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4)),
                    BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(8)), payload));
            }
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException or InvalidOperationException)
        { received.Writer.TryComplete(); }
    }

    public void Dispose() => socket.Dispose();
}
