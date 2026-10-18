using Fantasy;
using MongoDB.Driver;

if (args is ["--cleanup-db", var database])
{
    if (!System.Text.RegularExpressions.Regex.IsMatch(database, "^fantasy_sync_review_[a-f0-9]{32}$"))
        throw new ArgumentException("Only an isolated integration database can be removed.");
    await new MongoClient("mongodb://127.0.0.1:27017").DropDatabaseAsync(database);
    return 0;
}

const string password = "SyncReview_Only_2026";
var suffix = Guid.NewGuid().ToString("N")[..12];
var firstAccount = "sync_a_" + suffix;
var secondAccount = "sync_b_" + suffix;

try
{
    if (args.Length > 0 && args[0] == "--peer")
    {
        using var peer = await LoginAsync(args[1]);
        var entered = await EnterAsync(peer);
        await ReadyAsync(peer, entered.WorldSessionId);
        Console.WriteLine($"PEER_READY {args[1]} role={entered.Info.RoleId}");
        var duration = args.Length > 2 ? int.Parse(args[2]) : 60;
        var receivedMoves = new List<G2C_PlayerMove>();
        uint epoch = 0;
        using var observation = new CancellationTokenSource();
        var receiver = ObserveAsync();
        for (uint sequence = 1; sequence <= duration * 10; sequence++)
        {
            var angle = sequence * 0.06f;
            await MoveAsync(peer, entered.WorldSessionId, sequence, entered.Info.Movement.X + MathF.Sin(angle) * 2,
                entered.Info.Movement.Y, entered.Info.Movement.Z + MathF.Cos(angle) * 2 - 2,
                movementEpoch: epoch, rotationY: 90 + angle * 180 / MathF.PI);
            await Task.Delay(100);
        }
        observation.Cancel();
        await receiver;
        foreach (var group in receivedMoves.GroupBy(m => m.RoleId))
        {
            var firstMove = group.First().Movement;
            var lastMove = group.Last().Movement;
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                roleId = group.Key,
                snapshots = group.Count(),
                states = group.Select(m => m.Movement.State.ToString()).Distinct(),
                maxSpeed = group.Max(m => m.Movement.Speed),
                start = new[] { firstMove.X, firstMove.Y, firstMove.Z },
                end = new[] { lastMove.X, lastMove.Y, lastMove.Z },
                lastVersion = group.Last().Version,
            }));
        }
        return 0;

        async Task ObserveAsync()
        {
            while (!observation.IsCancellationRequested)
            {
                var frame = await peer.WaitAsync(_ => true, 250);
                if (frame?.Opcode == OuterOpcode.G2C_PlayerMove)
                {
                    var movement = frame.Value.Read<G2C_PlayerMove>();
                    Require(movement.WorldSessionId == entered.WorldSessionId, "peer received its own world lease");
                    receivedMoves.Add(movement);
                }
                else if (frame?.Opcode == OuterOpcode.G2C_PlayerPositionCorrection)
                    epoch = frame.Value.Read<G2C_PlayerPositionCorrection>().MovementEpoch;
            }
        }
    }

    using var first = await LoginAsync(firstAccount);
    var a = await EnterAsync(first);
    await ReadyAsync(first, a.WorldSessionId);
    using var second = await LoginAsync(secondAccount);
    var b = await EnterAsync(second);

    await MoveAsync(second, b.WorldSessionId, 1, 0.5f);
    Require(await first.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerEntered || f.Opcode == OuterOpcode.G2C_PlayerMove, 250) == null,
        "loading player is invisible and cannot move");
    var wrongReady = await second.CallAsync<C2G_WorldReadyRequest, G2C_WorldReadyResponse>(new() { WorldSessionId = b.WorldSessionId + 1 });
    Require(wrongReady.Result == ResultCode.InvalidWorldSession, "wrong world lease is rejected");
    var baseline = await ReadyAsync(second, b.WorldSessionId);
    Require(baseline.Players.Any(p => p.RoleId == a.Info.RoleId && p.SpawnId == a.WorldSessionId), "ready baseline includes existing player");
    var spawn = await first.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerEntered);
    var initialPresence = spawn?.Read<G2C_PlayerEntered>().Player;
    Require(initialPresence?.SpawnId == b.WorldSessionId && initialPresence.SpawnRevision > 0, "ready publishes ordered entered event");
    await ReadyAsync(second, b.WorldSessionId);
    Require(await first.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerEntered, 200) == null, "ready is idempotent");
    Pass("prepare -> ready visibility, identity and idempotence");

    await MoveAsync(second, b.WorldSessionId, 2, 0.5f);
    var moved = await first.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerMove);
    Require(moved?.Read<G2C_PlayerMove>() is { } move && move.WorldSessionId == a.WorldSessionId
        && move.SpawnId == b.WorldSessionId && move.SpawnRevision == initialPresence.SpawnRevision
        && move.Movement.X == 0.5f && move.ServerTimeMs > 0, "real movement routed to correct world");
    await MoveAsync(second, b.WorldSessionId, 3, 1000);
    var correction = await second.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerPositionCorrection);
    var corrected = correction?.Read<G2C_PlayerPositionCorrection>();
    Require(corrected != null && corrected.AckSequence == 3 && corrected.MovementEpoch == 1
        && corrected.WorldSessionId == b.WorldSessionId && corrected.Movement.X == 0.5f, "teleport is rejected and corrected");
    await MoveAsync(second, b.WorldSessionId, 2, 0);
    await MoveAsync(second, b.WorldSessionId + 1, 4, 0);
    Require(await first.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerMove, 250) == null, "stale sequence and wrong lease cannot move");
    // 即使位置合法，回正前发出的在途旧代次包也不能重新改变已确认位置。
    await MoveAsync(second, b.WorldSessionId, 5, 0.75f);
    Require(await first.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerMove, 150) == null, "old correction epoch cannot move");
    var resent = await second.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerPositionCorrection
        && f.Read<G2C_PlayerPositionCorrection>().MovementEpoch == 1, 1000);
    Require(resent != null && resent.Value.Read<G2C_PlayerPositionCorrection>().Movement.X == 0.5f,
        "lost correction can be recovered from old-epoch heartbeat");
    await MoveAsync(second, b.WorldSessionId, 6, 0.75f, movementEpoch: corrected!.MovementEpoch);
    var resumed = await first.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerMove);
    Require(resumed?.Read<G2C_PlayerMove>().Movement.X == 0.75f, "new epoch resumes normal movement");
    Pass("movement broadcast, teleport correction and stale-message rejection");

    second.Dispose();
    var left = await first.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerLeft);
    Require(left?.Read<G2C_PlayerLeft>().SpawnId == b.WorldSessionId, "disconnect publishes matching incarnation");
    using var reconnected = await LoginAsync(secondAccount);
    var c = await EnterAsync(reconnected);
    Require(c.WorldSessionId != b.WorldSessionId && c.Info.RoleId == b.Info.RoleId
        && c.Info.Movement.X == 0.75f, "final position survives reconnect with a new incarnation");
    await ReadyAsync(reconnected, c.WorldSessionId);
    var respawn = await first.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerEntered);
    Require(respawn?.Read<G2C_PlayerEntered>().Player.SpawnRevision > initialPresence.SpawnRevision,
        "reconnect advances spawn order");
    await MoveAsync(reconnected, c.WorldSessionId, 1, 1);
    Require(await first.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerMove) != null, "new connection sequence restarts safely");
    Pass("disconnect persistence and fresh upstream sequence");

    using var replacement = await LoginAsync(secondAccount);
    Require(await reconnected.WaitAsync(f => f.Opcode == OuterOpcode.G2C_RepeatLogin) != null, "duplicate login notifies the old connection");
    var d = await EnterAsync(replacement);
    await ReadyAsync(replacement, d.WorldSessionId);
    var denied = await reconnected.CallAsync<C2G_EnterGameRequest, G2C_EnterGameResponse>(new());
    Require(denied.Result == ResultCode.NotAuthenticated, "replaced connection loses authority");
    Pass("duplicate login invalidates old authority");
    Console.WriteLine("4/4 real-network scenarios passed.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    return 1;
}

static void Require(bool condition, string message)
{ if (!condition) throw new InvalidOperationException(message); }
static void Pass(string name) => Console.WriteLine("PASS " + name);

static async Task<TcpTestClient> LoginAsync(string account)
{
    string address;
    string token;
    using (var registration = await TcpTestClient.ConnectAsync(LoginServerConfig.GetAddress(account)))
    {
        var registered = await registration.CallAsync<C2A_RegisterRequest, A2C_RegisterResponse>(new() { Username = account, Password = password });
        Require(registered.Result is ResultCode.Success or ResultCode.AccountExists, "registration: " + registered.Result);
    }
    using (var authentication = await TcpTestClient.ConnectAsync(LoginServerConfig.GetAddress(account)))
    {
        var authenticated = await authentication.CallAsync<C2A_LoginRequest, A2C_LoginResponse>(new() { Username = account, Password = password });
        Require(authenticated.Result == ResultCode.Success, "authentication: " + authenticated.Result);
        address = authenticated.GateAddress;
        token = authenticated.Token;
    }
    var gate = await TcpTestClient.ConnectAsync(address);
    var unsupported = await gate.CallAsync<C2G_LoginRequest, G2C_LoginResponse>(new() { Token = token });
    Require(unsupported.Result == ResultCode.UnsupportedProtocol, "older client version must fail explicitly");
    var login = await gate.CallAsync<C2G_LoginRequest, G2C_LoginResponse>(new() { Token = token, ProtocolVersion = (uint)ProtocolRevision.Current });
    Require(login.Result == ResultCode.Success, "gate login: " + login.Result);
    return gate;
}

static async Task<G2C_EnterGameResponse> EnterAsync(TcpTestClient gate)
{
    var entered = await gate.CallAsync<C2G_EnterGameRequest, G2C_EnterGameResponse>(new());
    if (entered.Result == ResultCode.RoleNotFound)
    {
        var created = await gate.CallAsync<C2G_CreateRoleRequest, G2C_CreateRoleResponse>(new() { NickName = "同步测试" });
        Require(created.Result == ResultCode.Success, "create role");
        entered = await gate.CallAsync<C2G_EnterGameRequest, G2C_EnterGameResponse>(new());
    }
    Require(entered.Result == ResultCode.Success && entered.WorldSessionId != 0 && entered.Info?.RoleId != 0, "enter game");
    return entered;
}

static async Task<G2C_WorldReadyResponse> ReadyAsync(TcpTestClient gate, long worldSessionId)
{
    var result = await gate.CallAsync<C2G_WorldReadyRequest, G2C_WorldReadyResponse>(new() { WorldSessionId = worldSessionId });
    Require(result.Result == ResultCode.Success, "world ready: " + result.Result);
    return result;
}

static Task MoveAsync(TcpTestClient gate, long worldSessionId, uint sequence, float x, float y = 0, float z = 0,
    uint movementEpoch = 0, float rotationY = 0)
    => gate.SendAsync(new C2G_PlayerMove { WorldSessionId = worldSessionId, Sequence = sequence,
        MovementEpoch = movementEpoch,
        Movement = new MovementSnapshot { X = x, Y = y, Z = z, RotationY = rotationY, State = PlayerMotionState.Run, Speed = 6.5f } });
