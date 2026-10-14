using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Xml.Linq;
using Fantasy;
using Fantasy.MongdbModel;
using Fantasy.Platform.Net;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using LightProto;

// 检查离线数据和规则，不将这些对象加入 Fantasy 场景或对象池。
#pragma warning disable FANTASY002

// 回归检查只调用规则和数据模型，不启动服务器、不访问数据库。
var checks = new (string Name, Action Run)[]
{
    ("Role responses contain independent DTOs", CheckRoleMapping),
    ("Database snapshots exclude session and runtime movement fields", CheckPersistence),
    ("Normal movement and running remain valid", CheckNormalMovement),
    ("All five motion states are accepted independently of Animator numbers", CheckMotionStates),
    ("High packet frequency cannot refill movement allowance", CheckMovementBurst),
    ("Idle time cannot accumulate a teleport allowance", CheckIdleTeleport),
    ("Invalid numbers, speed and states preserve the accepted position", CheckInvalidMovement),
    ("Older movement packets cannot roll back a newer snapshot", CheckOldSequence),
    ("Movement sequence and version survive uint wraparound", CheckSequenceWrap),
    ("Reconnect resets upstream sequence without resetting downstream version", CheckReconnect),
    ("Passwords use random salt and retain legacy compatibility", CheckPasswords),
    ("Gate rejects expired, mismatched and incorrectly signed tokens", CheckTokens),
    ("Client applies only newer corrections and clears data on logout", CheckClientCorrections),
    ("Client protocol helper contains only the six required sends", CheckClientProtocolHelper),
    ("Protocol results fail closed and public data excludes account identity", CheckProtocolContracts),
    ("Nested outer and inner messages survive real protobuf serialization", CheckProtocolSerialization),
    ("RPC helpers do not release a request reused while awaiting", CheckRequestOwnership),
};

var failures = 0;
foreach (var check in checks)
{
    try { check.Run(); Console.WriteLine($"PASS {check.Name}"); }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {check.Name}: {exception}");
    }
}
try
{
    await CheckConfigurationAsync();
    Console.WriteLine("PASS Canonical startup configuration loads without duplicate scene types");
}
catch (Exception exception)
{
    failures++;
    Console.Error.WriteLine($"FAIL startup configuration: {exception}");
}
var total = checks.Length + 1;
Console.WriteLine($"{total - failures}/{total} checks passed.");
return failures == 0 ? 0 : 1;

// 对外响应必须有完整数据；修改 DTO 不能修改在线实体。
static void CheckRoleMapping()
{
    var player = BsonSerializer.Deserialize<OnlineInfo>("{ '_id': 42 }");
    player.UserName = "account";
    player.NickName = "nickname";
    player.Hp = 90;
    player.MaxHp = 100;
    player.Mp = 60;
    player.MaxMp = 80;
    player.Gold = 123;
    player.X = 1;
    player.Y = 2;
    player.Z = 3;
    player.RotationY = 270;
    var info = PlayerMessageMapper.ToPlayerInfo(player);
    var presence = PlayerMessageMapper.ToPresence(player);
    Require(info.RoleId == 42 && info.NickName == "nickname", "identity");
    Require(info.Hp == 90 && info.MaxHp == 100 && info.Mp == 60 && info.MaxMp == 80 && info.Gold == 123, "stats");
    Require(info.Movement.X == 1 && info.Movement.Y == 2 && info.Movement.Z == 3 && info.Movement.RotationY == 270, "transform");
    presence.Movement.X = 99;
    Require(player.X == 1 && info.Movement.X == 1, "DTOs must not share mutable role data");
}

static void CheckPersistence()
{
    var player = NewPlayer();
    player.UserName = "account";
    Require(Apply(player, Move(10, 0.65f), 1100), "accepted movement");
    var copy = BsonSerializer.Deserialize<OnlineInfo>(player.ToBson());
    Require(copy.UserName == player.UserName && copy.X == player.X, "persistent data");
    Require(!copy.IsConnected && !copy.IsDirty && copy.MovementVersion == 0 && !copy.HasLastMoveSequence, "runtime data excluded");
}

static void CheckNormalMovement()
{
    var player = NewPlayer();
    for (uint step = 1; step <= 100; step++)
        Require(Apply(player, Move(step, step * 0.65f), 1000 + step * 100), "normal running");
    Require(player.IsDirty && player.MovementState == PlayerMotionState.Run && player.MovementVersion == 101, "accepted state");
    var jump = Move(101, player.X);
    jump.Movement.Y = 0.8f;
    jump.Movement.State = PlayerMotionState.Jump;
    jump.Movement.RotationY = -90;
    Require(Apply(player, jump, 11100), "jump");
    Require(player.RotationY == 270, "normalized rotation");
}

static void CheckMotionStates()
{
    foreach (var state in Enum.GetValues<PlayerMotionState>())
    {
        var player = NewPlayer();
        Require(PlayerMovementRules.TryApply(player, 1, new MovementSnapshot { State = state }, 1100), "motion state");
        Require(player.MovementState == state, "typed state retained");
    }
    Require((int)PlayerMotionState.Run == 3 && (int)PlayerMotionState.Fall == 4, "not Animator state numbers");
}

static void CheckMovementBurst()
{
    var player = NewPlayer();
    Require(Apply(player, Move(1, 0.6f), 1000), "initial allowance");
    var allowance = player.HorizontalAllowance;
    for (uint step = 2; step <= 20; step++)
        Require(!Apply(player, Move(step, 1.2f), 1000), "same-time extra movement");
    Require(player.X == 0.6f && player.HorizontalAllowance == allowance, "rejections must preserve budget");
    Require(Apply(player, Move(21, 1.25f), 1100), "budget recovers with elapsed time");
}

static void CheckIdleTeleport()
{
    var player = NewPlayer();
    Require(!Apply(player, Move(1, 100), 3601000), "long-idle teleport");
    Require(player.X == 0 && !player.IsDirty, "rejected teleport must not change saved data");
}

static void CheckInvalidMovement()
{
    var player = NewPlayer();
    var invalid = new[]
    {
        new G2Game_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { X = float.NaN } },
        new G2Game_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { Y = float.PositiveInfinity } },
        new G2Game_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { Z = float.NegativeInfinity } },
        new G2Game_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { RotationY = float.NaN } },
        new G2Game_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { Speed = float.NaN } },
        new G2Game_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { Speed = -1 } },
        new G2Game_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { Speed = 100 } },
        new G2Game_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { State = (PlayerMotionState)999 } },
        new G2Game_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { Y = 100 } },
        new G2Game_PlayerMove { Sequence = 1 },
        new G2Game_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { State = (PlayerMotionState)(-1) } },
    };
    foreach (var request in invalid)
        Require(!Apply(player, request, 1100), "invalid snapshot");
    Require(player.X == 0 && player.Y == 0 && player.Z == 0 && !player.IsDirty, "accepted position");
}

static void CheckOldSequence()
{
    var player = NewPlayer();
    Require(Apply(player, Move(10, 0.65f), 1100), "new packet");
    Require(!Apply(player, Move(9, 0), 1200), "old packet");
    Require(!Apply(player, Move(10, 0), 1200), "duplicate packet");
    Require(player.X == 0.65f && player.LastMoveSequence == 10, "no rollback");
}

static void CheckSequenceWrap()
{
    var player = NewPlayer();
    player.MovementVersion = uint.MaxValue;
    Require(Apply(player, Move(uint.MaxValue, 0.6f), 1100), "last uint sequence");
    Require(player.MovementVersion == 0, "version wrap");
    Require(Apply(player, Move(0, 1.2f), 1200), "sequence wrap");
    Require(!Apply(player, Move(uint.MaxValue, 0.6f), 1300), "old sequence after wrap");
}

static void CheckReconnect()
{
    var player = NewPlayer();
    Require(Apply(player, Move(20, 0.65f), 1100), "movement before reconnect");
    var version = player.MovementVersion;
    player.BindGateSession(2, 100);
    Require(!player.HasLastMoveSequence && player.MovementVersion == unchecked(version + 1) && player.MovementState == PlayerMotionState.Idle && player.X == 0.65f, "new session");
    player.LastMoveReceivedAt = 1100;
    Require(Apply(player, Move(1, 1.3f), 1200), "new upstream sequence");
    player.BindGateSession(2, 100);
    Require(player.LastMoveSequence == 1 && player.HasLastMoveSequence, "repeated binding is idempotent");
}

static void CheckPasswords()
{
    var first = PasswordHasher.Hash("password");
    var second = PasswordHasher.Hash("password");
    Require(first != second && PasswordHasher.IsHashed(first), "independent salts");
    Require(PasswordHasher.Verify("password", first), "correct hashed password");
    Require(!PasswordHasher.Verify("wrong", first), "wrong hashed password");
    Require(PasswordHasher.Verify("legacy", "legacy") && !PasswordHasher.IsHashed("legacy"), "legacy compatibility");
    Require(!PasswordHasher.Verify("wrong", "legacy"), "wrong legacy password");
    Require(!PasswordHasher.Verify("password", "pbkdf2-sha256$600000$bad$bad"), "malformed stored hash");
    Require(PasswordHasher.VerifyAsync("password", first).GetAwaiter().GetResult(), "background verification");
    var backgroundHash = PasswordHasher.HashAsync("background").GetAwaiter().GetResult();
    Require(PasswordHasher.Verify("background", backgroundHash), "background hashing");
}

// 使用临时密钥调用生产 Awake 和验证方法，不读取部署密钥。
static void CheckTokens()
{
    var directory = Path.Combine(Path.GetTempPath(), "mmo-regression-" + Guid.NewGuid().ToString("N"));
    var previous = Environment.GetEnvironmentVariable("FANTASY_JWT_KEY_DIRECTORY");
    var authentication = new AuthenticationJwtComponent();
    var gate = new GateJwtComponent();
    Directory.CreateDirectory(directory);
    try
    {
        using var rsa = RSA.Create(2048);
        File.WriteAllBytes(Path.Combine(directory, "private.key"), rsa.ExportRSAPrivateKey());
        File.WriteAllBytes(Path.Combine(directory, "public.key"), rsa.ExportRSAPublicKey());
        Environment.SetEnvironmentVariable("FANTASY_JWT_KEY_DIRECTORY", directory);
        new AuthenticationJwtAwakeSystem().Invoke(authentication);
        new GateJwtAwakeSystem().Invoke(gate);
        var token = authentication.GenerateJwtTokens("account", 1002);
        Require(gate.TryValidateToken(token, 1002, out var account) && account == "account", "valid token");
        Require(!gate.TryValidateToken(token, 1003, out _), "wrong account");
        Require(!gate.TryValidateToken("invalid", 1002, out _), "malformed token");
        Require(!gate.TryValidateToken("", 1002, out _), "empty token");
        var expired = new JwtSecurityToken(issuer: "Fzy", audience: "Fzy",
            claims: new[] { new Claim("UserName", "account"), new Claim("GateSceneId", "1002") },
            notBefore: DateTime.UtcNow.AddMinutes(-5), expires: DateTime.UtcNow.AddMinutes(-2),
            signingCredentials: authentication.SigningCredentials);
        Require(!gate.TryValidateToken(new JwtSecurityTokenHandler().WriteToken(expired), 1002, out _), "expired token");
        using var unrelatedKey = RSA.Create(2048);
        var forged = new JwtSecurityToken(issuer: "Fzy", audience: "Fzy",
            claims: new[] { new Claim("UserName", "account"), new Claim("GateSceneId", "1002") }, expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new RsaSecurityKey(unrelatedKey), SecurityAlgorithms.RsaSha256));
        Require(!gate.TryValidateToken(new JwtSecurityTokenHandler().WriteToken(forged), 1002, out _), "wrong signature");
    }
    finally
    {
        authentication.Rsa?.Dispose();
        gate.Rsa?.Dispose();
        Environment.SetEnvironmentVariable("FANTASY_JWT_KEY_DIRECTORY", previous);
        File.Delete(Path.Combine(directory, "private.key"));
        File.Delete(Path.Combine(directory, "public.key"));
        Directory.Delete(directory);
    }
}

static void CheckClientCorrections()
{
    var model = new PlayerSelfModel();
    model.SetInfo(new PlayerInfo { RoleId = 42, NickName = "nickname", Hp = 90, Gold = 123, Movement = new MovementSnapshot() });
    var notifications = 0;
    model.PositionCorrected += (_, _, _, _) => notifications++;
    model.CorrectPosition(1, new MovementSnapshot { X = 10, Y = 0, Z = 0, RotationY = 0 });
    Require(notifications == 0, "correction before world readiness");
    model.IsInWorld = true;
    model.CorrectPosition(uint.MaxValue, new MovementSnapshot { X = 1, Y = 2, Z = 3, RotationY = 90 });
    model.CorrectPosition(0, new MovementSnapshot { X = 4, Y = 5, Z = 6, RotationY = 180 });
    model.CorrectPosition(uint.MaxValue, new MovementSnapshot { X = 1, Y = 2, Z = 3, RotationY = 90 });
    model.CorrectPosition(0, new MovementSnapshot { X = 99, Y = 99, Z = 99, RotationY = 0 });
    Require(notifications == 2 && model.X == 4 && model.RotationY == 180, "only newer correction");
    model.Clear();
    Require(!model.HasRole && !model.IsInWorld && model.Hp == 0 && model.Gold == 0 && model.NickName == null, "logout reset");
    model.IsInWorld = true;
    model.CorrectPosition(0, new MovementSnapshot { X = 1, Y = 0, Z = 0, RotationY = 0 });
    Require(notifications == 3 && model.X == 1, "correction version resets for a new login");
    model.SetInfo(new PlayerInfo { RoleId = 43, Movement = new MovementSnapshot() });
    model.CorrectPosition(0, new MovementSnapshot { X = 8 });
    Require(notifications == 4 && model.X == 8, "re-entering the world resets correction acknowledgement");
}

// 检查结构职责，避免重新引入账号广播和重复角色 DTO。
static void CheckProtocolContracts()
{
    Require(new A2C_RegisterResponse().Result != ResultCode.Success, "missing result must not mean success");
    Require(new G2C_EnterGameResponse().Result == ResultCode.Unknown, "default result");
    Require(typeof(PlayerPresence).GetProperty("UserName") == null
        && typeof(PlayerInfo).GetProperty("UserName") == null, "no account exposure");
    Require(typeof(G2C_CreateRoleResponse).GetProperty("Info") == null, "create result only");
    Require(typeof(G2Game_CreateRoleRequest).GetProperty("GateSessionRuntimeId") == null, "create does not bind session");
    Require(typeof(C2G_LoginRequest).GetProperty("UserName") == null, "identity comes from signed token");
    Require(typeof(Game2G_EnterGameResponse).GetProperty("Info")?.PropertyType == typeof(PlayerInfo), "shared DTO");
    Require(typeof(G2C_PlayerPositionCorrection).GetProperty("AckSequence") != null
        && typeof(G2C_PlayerMove).GetProperty("Version") != null, "separate correction and broadcast");
}

// 使用生成的序列化器而非模拟 DTO，检查字段编号和嵌套数据。
static void CheckProtocolSerialization()
{
    var movement = new MovementSnapshot { X = 1, Y = 2, Z = 3, RotationY = 90,
        State = PlayerMotionState.Run, Speed = 6.5f };
    var request = Serializer.DeepClone(new C2G_PlayerMove { Sequence = uint.MaxValue, Movement = movement });
    Require(request.Sequence == uint.MaxValue && request.Movement.State == PlayerMotionState.Run
        && request.Movement.X == 1, "movement round trip");
    var entered = Serializer.DeepClone(new Game2G_PlayerEntered { GateSessionRuntimeId = 123,
        Player = new PlayerPresence { RoleId = 42, NickName = "nickname", Version = 8, Movement = movement } });
    Require(entered.Player.Version == 8 && entered.Player.Movement.Speed == 6.5f
        && entered.GateSessionRuntimeId == 123, "inner shared DTO round trip");
    var response = Serializer.DeepClone(new Game2G_EnterGameResponse { Result = ResultCode.Success,
        ErrorCode = 17, Info = new PlayerInfo { RoleId = 42, Gold = 100, Movement = movement } });
    Require(response.Result == ResultCode.Success && response.ErrorCode == 17
        && response.Info.Gold == 100, "business result and framework error remain independent");
    var correction = Serializer.DeepClone(new G2C_PlayerPositionCorrection { AckSequence = 9, Movement = movement });
    Require(correction.AckSequence == 9 && correction.Movement.RotationY == 90, "correction round trip");
}

// 模拟 Fantasy 已回收请求、其他请求再次租用同一个实例的时序。
static void CheckRequestOwnership()
{
    var session = new RequestReuseSession();
    var pending = session.C2A_LoginRequest("account", "password");
    session.Complete();
    using var response = pending.GetAwaiter().GetResult();
    Require(session.Reused.Username == "new-request" && session.Reused.IsPool(), "await must not release reused request");
    session.Reused.Dispose();
}

// 防止全量协议导出重新加入未使用的重载和服务端发送入口。
static void CheckClientProtocolHelper()
{
    var expected = new[]
    {
        nameof(NetworkProtocolHelper.C2A_RegisterRequest),
        nameof(NetworkProtocolHelper.C2A_LoginRequest),
        nameof(NetworkProtocolHelper.C2G_LoginRequest),
        nameof(NetworkProtocolHelper.C2G_EnterGameRequest),
        nameof(NetworkProtocolHelper.C2G_CreateRoleRequest),
        nameof(NetworkProtocolHelper.C2G_PlayerMove),
    };
    var methods = typeof(NetworkProtocolHelper).GetMethods(
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly);
    Require(methods.Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal)
        .SequenceEqual(expected.OrderBy(name => name, StringComparer.Ordinal)), "only required client sends");
    foreach (var method in methods)
    {
        var parameters = method.GetParameters();
        Require(parameters[0].ParameterType == typeof(Fantasy.Network.Session), "session extension");
        Require(parameters.Skip(1).All(parameter => parameter.ParameterType.IsPrimitive
            || parameter.ParameterType == typeof(string) || parameter.ParameterType == typeof(MovementSnapshot)), "only actual payloads, no unused request-object overload");
    }
}

// 只加载配置表，不启动进程、网络或数据库。
static async Task CheckConfigurationAsync()
{
    var path = Path.Combine(AppContext.BaseDirectory, "Fantasy.config");
    var xml = XDocument.Load(path);
    XNamespace ns = "http://fantasy.net/config";
    Require(xml.Root?.Element(ns + "controlCenter")?.Attribute("enabled")?.Value != "true", "test must not contact Control Center");
    Log.Initialize("Regression");
    await ConfigLoader.InitializeFromXml(path);
    Require(SceneConfigData.Instance.GetSceneBySceneType(SceneType.Authentication).Count > 0, "authentication routes");
    Require(SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate).Count > 0, "gate routes");
    Require(SceneConfigData.Instance.GetSceneBySceneType(SceneType.Game).Count > 0, "game routes");
}

// 固定服务端时钟，使预算检查不依赖实际等待时间。
static OnlineInfo NewPlayer()
{
    var player = new OnlineInfo();
    player.BindGateSession(1, 100);
    player.LastMoveReceivedAt = 1000;
    return player;
}

static G2Game_PlayerMove Move(uint sequence, float x) => new()
{
    Sequence = sequence, Movement = new MovementSnapshot { X = x, State = PlayerMotionState.Run, Speed = 6.5f },
};

// 测试通过同一入口提交协议采样，生产规则不依赖整条路由消息。
static bool Apply(OnlineInfo player, G2Game_PlayerMove request, long now)
    => PlayerMovementRules.TryApply(player, request.Sequence, request.Movement, now);

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class RequestReuseSession : Session
{
    private readonly FTask<IResponse> completion = FTask<IResponse>.Create(false);
    public C2A_LoginRequest Reused { get; private set; } = null!;

    public override FTask<IResponse> Call<T>(T request, long address = 0)
    {
        request.Dispose();
        Reused = C2A_LoginRequest.Create();
        RequireReuse(ReferenceEquals(request, Reused));
        Reused.Username = "new-request";
        return completion;
    }

    public void Complete() => completion.SetResult(new A2C_LoginResponse { Result = ResultCode.Success });
    private static void RequireReuse(bool reused)
    {
        if (!reused) throw new InvalidOperationException("Test did not reuse the pooled request.");
    }
}
