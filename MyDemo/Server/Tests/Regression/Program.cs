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

// 检查离线数据和规则，不将这些对象加入 Fantasy 场景或对象池。
#pragma warning disable FANTASY002

// 回归检查只调用规则和数据模型，不启动服务器、不访问数据库。
var checks = new (string Name, Action Run)[]
{
    ("Role responses contain independent DTOs", CheckRoleMapping),
    ("Database snapshots exclude session and runtime movement fields", CheckPersistence),
    ("Normal movement and running remain valid", CheckNormalMovement),
    ("High packet frequency cannot refill movement allowance", CheckMovementBurst),
    ("Idle time cannot accumulate a teleport allowance", CheckIdleTeleport),
    ("Invalid numbers, speed and states preserve the accepted position", CheckInvalidMovement),
    ("Older movement packets cannot roll back a newer snapshot", CheckOldSequence),
    ("Movement sequence and version survive uint wraparound", CheckSequenceWrap),
    ("Reconnect resets upstream sequence without resetting downstream version", CheckReconnect),
    ("Passwords use random salt and retain legacy compatibility", CheckPasswords),
    ("Gate rejects expired, mismatched and incorrectly signed tokens", CheckTokens),
    ("Client applies only newer corrections and clears data on logout", CheckClientCorrections),
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
    var data = PlayerMessageMapper.ToRoleData(player);
    var info = PlayerMessageMapper.ToPlayerInfo(data);
    Require(info.RoleId == 42 && info.UserName == "account" && info.NickName == "nickname", "identity");
    Require(info.Hp == 90 && info.MaxHp == 100 && info.Mp == 60 && info.MaxMp == 80 && info.Gold == 123, "stats");
    Require(info.X == 1 && info.Y == 2 && info.Z == 3 && info.RotationY == 270, "transform");
    data.X = 99;
    Require(player.X == 1 && info.X == 1, "DTOs must not share mutable role data");
}

static void CheckPersistence()
{
    var player = NewPlayer();
    player.UserName = "account";
    Require(PlayerMovementRules.TryApply(player, Move(10, 0.65f), 1100), "accepted movement");
    var copy = BsonSerializer.Deserialize<OnlineInfo>(player.ToBson());
    Require(copy.UserName == player.UserName && copy.X == player.X, "persistent data");
    Require(!copy.IsConnected && !copy.IsDirty && copy.MovementVersion == 0 && !copy.HasLastMoveSequence, "runtime data excluded");
}

static void CheckNormalMovement()
{
    var player = NewPlayer();
    for (uint step = 1; step <= 100; step++)
        Require(PlayerMovementRules.TryApply(player, Move(step, step * 0.65f), 1000 + step * 100), "normal running");
    Require(player.IsDirty && player.MovementState == 6 && player.MovementVersion == 100, "accepted state");
    var jump = Move(101, player.X);
    jump.Y = 0.8f;
    jump.State = 2;
    jump.RotationY = -90;
    Require(PlayerMovementRules.TryApply(player, jump, 11100), "jump");
    Require(player.RotationY == 270, "normalized rotation");
}

static void CheckMovementBurst()
{
    var player = NewPlayer();
    Require(PlayerMovementRules.TryApply(player, Move(1, 0.6f), 1000), "initial allowance");
    var allowance = player.HorizontalAllowance;
    for (uint step = 2; step <= 20; step++)
        Require(!PlayerMovementRules.TryApply(player, Move(step, 1.2f), 1000), "same-time extra movement");
    Require(player.X == 0.6f && player.HorizontalAllowance == allowance, "rejections must preserve budget");
    Require(PlayerMovementRules.TryApply(player, Move(21, 1.25f), 1100), "budget recovers with elapsed time");
}

static void CheckIdleTeleport()
{
    var player = NewPlayer();
    Require(!PlayerMovementRules.TryApply(player, Move(1, 100), 3601000), "long-idle teleport");
    Require(player.X == 0 && !player.IsDirty, "rejected teleport must not change saved data");
}

static void CheckInvalidMovement()
{
    var player = NewPlayer();
    var invalid = new[]
    {
        new G2Game_PlayerMove { Sequence = 1, X = float.NaN },
        new G2Game_PlayerMove { Sequence = 1, Y = float.PositiveInfinity },
        new G2Game_PlayerMove { Sequence = 1, Z = float.NegativeInfinity },
        new G2Game_PlayerMove { Sequence = 1, RotationY = float.NaN },
        new G2Game_PlayerMove { Sequence = 1, Speed = float.NaN },
        new G2Game_PlayerMove { Sequence = 1, Speed = -1 },
        new G2Game_PlayerMove { Sequence = 1, Speed = 100 },
        new G2Game_PlayerMove { Sequence = 1, State = 999 },
        new G2Game_PlayerMove { Sequence = 1, Y = 100 },
    };
    foreach (var request in invalid)
        Require(!PlayerMovementRules.TryApply(player, request, 1100), "invalid snapshot");
    Require(player.X == 0 && player.Y == 0 && player.Z == 0 && !player.IsDirty, "accepted position");
}

static void CheckOldSequence()
{
    var player = NewPlayer();
    Require(PlayerMovementRules.TryApply(player, Move(10, 0.65f), 1100), "new packet");
    Require(!PlayerMovementRules.TryApply(player, Move(9, 0), 1200), "old packet");
    Require(!PlayerMovementRules.TryApply(player, Move(10, 0), 1200), "duplicate packet");
    Require(player.X == 0.65f && player.LastMoveSequence == 10, "no rollback");
}

static void CheckSequenceWrap()
{
    var player = NewPlayer();
    player.MovementVersion = uint.MaxValue;
    Require(PlayerMovementRules.TryApply(player, Move(uint.MaxValue, 0.6f), 1100), "last uint sequence");
    Require(player.MovementVersion == 0, "version wrap");
    Require(PlayerMovementRules.TryApply(player, Move(0, 1.2f), 1200), "sequence wrap");
    Require(!PlayerMovementRules.TryApply(player, Move(uint.MaxValue, 0.6f), 1300), "old sequence after wrap");
}

static void CheckReconnect()
{
    var player = NewPlayer();
    Require(PlayerMovementRules.TryApply(player, Move(20, 0.65f), 1100), "movement before reconnect");
    var version = player.MovementVersion;
    player.BindGateSession(2, 100);
    Require(!player.HasLastMoveSequence && player.MovementVersion == version && player.X == 0.65f, "new session");
    player.LastMoveReceivedAt = 1100;
    Require(PlayerMovementRules.TryApply(player, Move(1, 1.3f), 1200), "new upstream sequence");
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
        var token = authentication.GenerateJwtTokens("account", "127.0.0.1:20001");
        Require(gate.ValidateJwtTokens(token, "account"), "valid token");
        Require(!gate.ValidateJwtTokens(token, "other"), "wrong account");
        Require(!gate.ValidateJwtTokens("invalid", "account"), "malformed token");
        Require(!gate.ValidateJwtTokens("", "account"), "empty token");
        var expired = new JwtSecurityToken(issuer: "Fzy", audience: "Fzy",
            claims: new[] { new Claim("UserName", "account") },
            notBefore: DateTime.UtcNow.AddMinutes(-5), expires: DateTime.UtcNow.AddMinutes(-2),
            signingCredentials: authentication.SigningCredentials);
        Require(!gate.ValidateJwtTokens(new JwtSecurityTokenHandler().WriteToken(expired), "account"), "expired token");
        using var unrelatedKey = RSA.Create(2048);
        var forged = new JwtSecurityToken(issuer: "Fzy", audience: "Fzy",
            claims: new[] { new Claim("UserName", "account") }, expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new RsaSecurityKey(unrelatedKey), SecurityAlgorithms.RsaSha256));
        Require(!gate.ValidateJwtTokens(new JwtSecurityTokenHandler().WriteToken(forged), "account"), "wrong signature");
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
    model.SetInfo(new PlayerInfo { RoleId = 42, UserName = "account", Hp = 90, Gold = 123 });
    var notifications = 0;
    model.PositionCorrected += (_, _, _, _) => notifications++;
    model.CorrectPosition(1, 10, 0, 0, 0);
    Require(notifications == 0, "correction before world readiness");
    model.IsInWorld = true;
    model.CorrectPosition(uint.MaxValue, 1, 2, 3, 90);
    model.CorrectPosition(0, 4, 5, 6, 180);
    model.CorrectPosition(uint.MaxValue, 1, 2, 3, 90);
    model.CorrectPosition(0, 99, 99, 99, 0);
    Require(notifications == 2 && model.X == 4 && model.RotationY == 180, "only newer correction");
    model.Clear();
    Require(!model.HasRole && !model.IsInWorld && model.Hp == 0 && model.Gold == 0 && model.UserName == null, "logout reset");
    model.IsInWorld = true;
    model.CorrectPosition(0, 1, 0, 0, 0);
    Require(notifications == 3 && model.X == 1, "correction version resets for a new login");
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
    Sequence = sequence, X = x, State = 6, Speed = 6.5f,
};

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
