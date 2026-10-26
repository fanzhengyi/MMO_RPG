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
    if (args.Length > 0 && args[0] == "--combat-peer")
        return await RunCombatPeerAsync(args[1], args.Length > 2 ? int.Parse(args[2]) : 120);

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
    var loadingAction = await ActionAsync(second, b.WorldSessionId, 1, PlayerActionKind.Skill01);
    Require(loadingAction.Result == ResultCode.InvalidWorldSession, "loading player cannot start actions");
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

    var action = await ActionAsync(second, b.WorldSessionId, 1, PlayerActionKind.Skill04);
    Require(action.Result == ResultCode.Success && action.Sequence == 1 && action.Action.Kind == PlayerActionKind.Skill04
        && action.Action.DurationMs == 1200 && action.Action.StartedAtServerTimeMs > 0,
        "skill accepted with authoritative timing");
    var actionFrame = await first.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerAction);
    var visibleAction = actionFrame?.Read<G2C_PlayerAction>();
    Require(visibleAction?.RoleId == b.Info.RoleId && visibleAction.WorldSessionId == a.WorldSessionId
        && visibleAction.SpawnId == b.WorldSessionId && visibleAction.SpawnRevision == initialPresence.SpawnRevision
        && visibleAction.Action.Version == action.Action.Version, "independent action broadcast preserves incarnation");
    var actionBaseline = await ReadyAsync(first, a.WorldSessionId);
    Require(actionBaseline.Players.Single(p => p.RoleId == b.Info.RoleId).Action.Sequence == 1,
        "fresh visibility baseline carries ongoing action");
    var busy = await ActionAsync(second, b.WorldSessionId, 2, PlayerActionKind.Slide);
    Require(busy.Result == ResultCode.Busy && busy.Sequence == 2 && busy.Action.Sequence == 1,
        "locked skill cannot be replaced by slide and rejection echoes exact request");
    Require(await first.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerAction, 150) == null,
        "rejected action is not broadcast");
    await Task.Delay(1200);
    var cooldown = await ActionAsync(second, b.WorldSessionId, 3, PlayerActionKind.Skill04);
    Require(cooldown.Result == ResultCode.Cooldown, "recovering does not reset skill cooldown");
    var nextSkill = await ActionAsync(second, b.WorldSessionId, 4, PlayerActionKind.Skill01);
    Require(nextSkill.Result == ResultCode.Success, "another ready skill can start");
    Require((await first.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerAction))?.Read<G2C_PlayerAction>().Action.Sequence == 4,
        "second action arrives immediately without movement traffic");
    var oldCancel = await ActionAsync(second, b.WorldSessionId, 5, PlayerActionKind.None, cancelSequence: 1);
    Require(oldCancel.Result == ResultCode.Busy && oldCancel.Action.Sequence == 4,
        "cancel for old prediction cannot cancel a newer action");
    var cancelled = await ActionAsync(second, b.WorldSessionId, 6, PlayerActionKind.None, cancelSequence: 4);
    Require(cancelled.Result == ResultCode.Success && cancelled.Action.Kind == PlayerActionKind.None,
        "matching action cancel accepted");
    Require((await first.WaitAsync(f => f.Opcode == OuterOpcode.G2C_PlayerAction))?.Read<G2C_PlayerAction>().Action.Kind == PlayerActionKind.None,
        "explicit cancellation reaches observers");
    var wrongWorldAction = await ActionAsync(second, b.WorldSessionId + 1, 7, PlayerActionKind.Attack01);
    Require(wrongWorldAction.Result == ResultCode.InvalidWorldSession, "wrong world cannot act");
    await Task.Delay(110); // 本组集中发出多个 RPC，留出动作令牌，分别验证 epoch 与编号规则。
    var oldEpochAction = await ActionAsync(second, b.WorldSessionId, 8, PlayerActionKind.Attack01, movementEpoch: 1);
    Require(oldEpochAction.Result == ResultCode.InvalidAction, "wrong correction epoch cannot act: " + oldEpochAction.Result);
    await Task.Delay(110);
    var invalidAction = await ActionAsync(second, b.WorldSessionId, 9, (PlayerActionKind)999);
    Require(invalidAction.Result == ResultCode.InvalidAction, "unknown action kind rejected");
    Pass("action routing, visibility baseline, lock, cooldown, exact cancellation and stale-world rejection");

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
    Console.WriteLine("5/5 real-network scenarios passed.");
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
    // authenticationAddress：只对鉴权入口加测试端口偏移，Gate 地址由隔离服务器真实返回。
    var authenticationAddress = System.Net.IPEndPoint.Parse(LoginServerConfig.GetAddress(account));
    authenticationAddress.Port += int.TryParse(Environment.GetEnvironmentVariable("MMO_TEST_PORT_OFFSET"), out var offset) ? offset : 0;
    using (var registration = await TcpTestClient.ConnectAsync(authenticationAddress.ToString()))
    {
        var registered = await registration.CallAsync<C2A_RegisterRequest, A2C_RegisterResponse>(new() { AccountUser = account, Password = password });
        Require(registered.Result is ResultCode.Success or ResultCode.AccountExists, "registration: " + registered.Result);
    }
    using (var authentication = await TcpTestClient.ConnectAsync(authenticationAddress.ToString()))
    {
        var authenticated = await authentication.CallAsync<C2A_LoginRequest, A2C_LoginResponse>(new() { AccountUser = account, Password = password });
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

static Task<G2C_PlayerActionResponse> ActionAsync(TcpTestClient gate, long worldSessionId, uint sequence,
    PlayerActionKind kind, uint cancelSequence = 0, uint movementEpoch = 0, float rotationY = 90)
    => gate.CallAsync<C2G_PlayerActionRequest, G2C_PlayerActionResponse>(new()
    {
        WorldSessionId = worldSessionId, Sequence = sequence, Kind = kind,
        CancelSequence = cancelSequence, MovementEpoch = movementEpoch, RotationY = rotationY,
    });

/// <summary>编辑器联调陪测端：真实收发动作与移动，不在测试端调用服务器规则或业务 Handler。</summary>
static async Task<int> RunCombatPeerAsync(string account, int durationSeconds)
{
    using var peer = await LoginAsync(account);
    var entered = await EnterAsync(peer);
    await ReadyAsync(peer, entered.WorldSessionId);
    Console.WriteLine($"COMBAT_PEER_READY account={account} role={entered.Info.RoleId} world={entered.WorldSessionId}");
    // pattern：按共享节奏演示三段连击、四个技能和一次滑行，再回到起点开始下一轮。
    var pattern = new[] { PlayerActionId.Attack01, PlayerActionId.Attack02, PlayerActionId.Attack03,
        PlayerActionId.Skill01, PlayerActionId.Skill02, PlayerActionId.Skill03, PlayerActionId.Skill04, PlayerActionId.Slide };
    var actionCounts = new Dictionary<string, int>();
    var receivedMoves = 0;
    var sentActions = 0;
    var cursor = 0;
    uint actionSequence = 0, movementSequence = 0, epoch = 0;
    var x = entered.Info.Movement.X;
    var y = entered.Info.Movement.Y;
    var z = entered.Info.Movement.Z;
    var homeX = x + 2f;
    var homeZ = z + 2f;
    var started = Environment.TickCount64;
    var lastTick = started;
    var nextActionAt = started + 1500;
    var activeUntil = started;
    var activeKind = PlayerActionId.None;
    while (Environment.TickCount64 - started < durationSeconds * 1000L)
    {
        var now = Environment.TickCount64;
        var delta = Math.Min((now - lastTick) / 1000f, 0.25f);
        lastTick = now;
        var oldX = x;
        var oldZ = z;
        if (activeKind == PlayerActionId.Slide && now < activeUntil)
            x -= PlayerActionRules.SlideSpeed * delta;
        else if (now >= activeUntil && cursor == 0)
        {
            x += Math.Clamp(homeX - x, -2f * delta, 2f * delta);
            z += Math.Clamp(homeZ - z, -2f * delta, 2f * delta);
        }

        if (now >= nextActionAt && (cursor != 0 || Math.Abs(homeX - x) < 0.05f))
        {
            var id = pattern[cursor];
            var response = await ActionAsync(peer, entered.WorldSessionId, ++actionSequence,
                (PlayerActionKind)id, movementEpoch: epoch, rotationY: id == PlayerActionId.Slide ? 270f : 225f);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                eventName = "sent_action", roleId = entered.Info.RoleId, kind = id.ToString(),
                sequence = response.Sequence, result = response.Result.ToString(), version = response.Action?.Version,
            }));
            now = Environment.TickCount64;
            if (response.Result == ResultCode.Success)
            {
                sentActions++;
                activeKind = id;
                var timing = PlayerActionRules.Get(id);
                activeUntil = now + timing.RecoverAtMs;
                nextActionAt = now + (id is PlayerActionId.Attack01 or PlayerActionId.Attack02
                    ? timing.ChainAtMs : timing.RecoverAtMs) + 40;
                cursor = (cursor + 1) % pattern.Length;
            }
            else nextActionAt = now + 250;
        }

        var speed = delta > 0 ? Math.Min(MathF.Sqrt((x - oldX) * (x - oldX) + (z - oldZ) * (z - oldZ)) / delta,
            PlayerActionRules.SlideSpeed) : 0f;
        await peer.SendAsync(new C2G_PlayerMove
        {
            Sequence = ++movementSequence, WorldSessionId = entered.WorldSessionId, MovementEpoch = epoch,
            Movement = new MovementSnapshot
            {
                X = x, Y = y, Z = z, RotationY = activeKind == PlayerActionId.Slide ? 270f : 225f,
                State = speed > 0.01f ? PlayerMotionState.Walk : PlayerMotionState.Idle, Speed = speed,
            },
        });
        // RPC 和观察共用同一收包者，避免并行 WaitAsync 抢走对方正在等待的响应。
        for (var index = 0; index < 64; index++)
        {
            var frame = await peer.WaitAsync(_ => true, 1);
            if (frame == null) break;
            if (frame.Value.Opcode == OuterOpcode.G2C_PlayerMove) receivedMoves++;
            else if (frame.Value.Opcode == OuterOpcode.G2C_PlayerPositionCorrection)
            {
                var correction = frame.Value.Read<G2C_PlayerPositionCorrection>();
                epoch = correction.MovementEpoch;
                x = correction.Movement.X; y = correction.Movement.Y; z = correction.Movement.Z;
                Console.WriteLine($"COMBAT_PEER_CORRECTION epoch={epoch}");
            }
            else if (frame.Value.Opcode == OuterOpcode.G2C_PlayerAction)
            {
                var action = frame.Value.Read<G2C_PlayerAction>();
                Require(action.WorldSessionId == entered.WorldSessionId, "observer world identity");
                var key = action.RoleId + ":" + action.Action.Kind;
                actionCounts[key] = actionCounts.GetValueOrDefault(key) + 1;
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
                {
                    eventName = "received_action", roleId = action.RoleId, kind = action.Action.Kind.ToString(),
                    version = action.Action.Version, sequence = action.Action.Sequence,
                    origin = new[] { action.Action.OriginX, action.Action.OriginY, action.Action.OriginZ },
                }));
            }
            else if (frame.Value.Opcode == OuterOpcode.G2C_PlayerEntered)
                Console.WriteLine("COMBAT_PEER_SPAWN role=" + frame.Value.Read<G2C_PlayerEntered>().Player.RoleId);
        }
        await Task.Delay(50);
    }
    Console.WriteLine("COMBAT_PEER_SUMMARY " + System.Text.Json.JsonSerializer.Serialize(new
        { sentActions, receivedMoves, receivedActions = actionCounts }));
    return 0;
}
