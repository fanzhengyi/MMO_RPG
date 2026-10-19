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
    ("Single attack finishes without automatic continuation", CheckAttackSingle),
    ("Attack input is buffered briefly and stores only one continuation", CheckAttackBuffer),
    ("Attack combo cycles through one, two, three and one", CheckAttackCycle),
    ("Attack continuation expires after its post-recovery grace period", CheckAttackGrace),
    ("Low frame rate consumes each attack input only once", CheckAttackFrameBoundary),
    ("Attack interruption and reset clear the combo and buffered input", CheckAttackReset),
    ("Paused attack timing does not progress or consume input", CheckAttackPause),
    ("Attack buffer validity is evaluated at the actual chain point", CheckAttackBufferDeadline),
    ("Late attack input never rewinds to an earlier chain point", CheckAttackLateInput),
    ("Attack recovery releases control before the animation tail ends", CheckAttackRecovery),
    ("Correction epochs reject in-flight pre-correction movement", CheckCorrectionEpoch),
    ("Spawn ordering rejects retired instances across repeated reconnects", CheckSpawnOrdering),
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
    ("Client protocol helper contains only the required sends", CheckClientProtocolHelper),
    ("World readiness and incarnation isolate loading and reconnects", CheckWorldReadiness),
    ("Movement limiter permits state bursts but bounds sustained traffic", CheckMovementLimiter),
    ("Stationary heartbeats do not dirty persistent role data", CheckIdlePersistence),
    ("Snapshot playback uses server time and bounds jitter and memory", CheckSnapshotTimeline),
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

// 固定时长使连击时序检查不依赖 Unity 帧率或真实等待时间。
static AttackComboCore NewAttackCombo() => new(new[]
{
    new StepTiming(1f, 0.5f, 0.75f), new StepTiming(1f, 0.5f, 0.75f), new StepTiming(1f, 0.5f, 0.75f),
}, 0.5f, 0.22f);

static void CheckAttackSingle()
{
    var combo = NewAttackCombo();
    Require(combo.Tick(0.01f, true, true) == 1 && combo.IsAttacking, "first click starts first attack");
    Require(combo.Tick(0.25f, false, true) == 0 && combo.NormalizedTime == 0.25f, "progress advances");
    Require(combo.Tick(0.5f, false, true) == 0 && !combo.IsAttacking && combo.CurrentStep == 1
        && combo.NormalizedTime == 0.75f, "recovery without input does not start a second attack");
    combo.Tick(0.51f, false, true);
    Require(combo.CurrentStep == 0 && combo.NormalizedTime == 0f, "expired idle combo clears its step");
}

static void CheckAttackBuffer()
{
    var early = NewAttackCombo();
    early.Tick(0.01f, true, true);
    early.Tick(0.125f, true, true);
    Require(early.Tick(0.375f, false, true) == 0 && early.IsAttacking, "old click expires before chain point");
    Require(early.Tick(0.25f, false, true) == 0 && !early.IsAttacking, "expired input cannot trigger a later continuation");

    var combo = NewAttackCombo();
    combo.Tick(0.01f, true, true);
    Require(combo.Tick(0.3125f, true, true) == 0, "recent input is stored before chain point");
    combo.Tick(0.0625f, true, true);
    Require(combo.Tick(0.125f, true, true) == 2 && combo.NormalizedTime == 0f, "buffer starts exactly one next attack at chain point");
    Require(combo.Tick(0.75f, false, true) == 0 && !combo.IsAttacking && combo.CurrentStep == 2,
        "repeated buffered clicks and chain-frame click cannot queue third attack");
}

static void CheckAttackCycle()
{
    var combo = NewAttackCombo();
    Require(combo.Tick(0.01f, true, true) == 1, "first stage");
    foreach (var expected in new[] { 2, 3, 1 })
    {
        combo.Tick(0.3125f, true, true);
        Require(combo.Tick(0.1875f, false, true) == expected && combo.IsAttacking, "ordered cycle at chain points");
    }
}

static void CheckAttackGrace()
{
    var combo = NewAttackCombo();
    combo.Tick(0.01f, true, true);
    combo.Tick(0.75f, false, true);
    Require(combo.Tick(0.25f, true, true) == 2, "click during grace resumes next stage");
    combo.Tick(0.75f, false, true);
    Require(combo.Tick(0.51f, true, true) == 1, "click after grace restarts from stage one");
    combo.Tick(0.75f, false, true);
    Require(combo.Tick(0.5f, true, true) == 2, "grace deadline is inclusive");
}

static void CheckAttackFrameBoundary()
{
    var buffered = NewAttackCombo();
    buffered.Tick(0.01f, true, true);
    buffered.Tick(0.3125f, true, true);
    Require(buffered.Tick(3.5f, true, true) == 2 && buffered.NormalizedTime == 0f,
        "long frame starts one buffered animation without skipping its beginning");
    Require(buffered.Tick(0.75f, false, true) == 0 && !buffered.IsAttacking, "same click cannot affect two stages");

    var boundary = NewAttackCombo();
    boundary.Tick(0.01f, true, true);
    Require(boundary.Tick(0.875f, true, true) == 2, "new click crossing recovery uses actual recovery-time grace");
    boundary.Reset();
    boundary.Tick(0.01f, true, true);
    Require(boundary.Tick(1.5f, true, true) == 1, "long frame beyond recovery grace restarts first stage");
}

static void CheckAttackReset()
{
    var combo = NewAttackCombo();
    combo.Tick(0.01f, true, true);
    combo.Tick(0.3125f, true, true);
    Require(combo.Tick(0f, true, false) == 0 && combo.CurrentStep == 0 && !combo.IsAttacking,
        "loss of attack permission cancels even when paused");
    Require(combo.Tick(1f, false, true) == 0 && combo.CurrentStep == 0, "interruption clears buffered input");
    Require(combo.Tick(0.01f, true, true) == 1, "interruption clears combo history");
    combo.Tick(0.3125f, true, true);
    combo.Reset();
    Require(combo.CurrentStep == 0 && !combo.IsAttacking && combo.NormalizedTime == 0f, "explicit reset clears all presentation state");
    Require(combo.Tick(1f, false, true) == 0 && combo.Tick(0.01f, true, true) == 1, "explicit reset clears continuation input");
}

static void CheckAttackPause()
{
    var combo = NewAttackCombo();
    Require(combo.Tick(0f, true, true) == 0 && combo.CurrentStep == 0, "paused idle does not consume attack");
    combo.Tick(0.01f, true, true);
    combo.Tick(0.3125f, false, true);
    Require(combo.Tick(0f, true, true) == 0 && combo.NormalizedTime == 0.3125f, "paused attack does not progress");
    Require(combo.Tick(0.4375f, false, true) == 0 && !combo.IsAttacking, "paused click did not buffer continuation");
}

static void CheckAttackBufferDeadline()
{
    var timings = new[] { new StepTiming(1f, 0.5f, 0.75f), new StepTiming(1f, 0.5f, 0.75f) };
    var exact = new AttackComboCore(timings, 0.5f, 0.25f);
    exact.Tick(0.01f, true, true);
    exact.Tick(0.25f, true, true);
    Require(exact.Tick(2f, false, true) == 2, "buffer expiring exactly at chain point survives a long frame");

    var expired = new AttackComboCore(timings, 0.5f, 0.25f);
    expired.Tick(0.01f, true, true);
    expired.Tick(0.125f, true, true);
    Require(expired.Tick(2f, false, true) == 0 && expired.CurrentStep == 0 && !expired.IsAttacking,
        "buffer already expired at chain point cannot start a skipped continuation");
}

static void CheckAttackLateInput()
{
    var combo = NewAttackCombo();
    combo.Tick(0.01f, true, true);
    Require(combo.Tick(0.5625f, true, true) == 2 && combo.NormalizedTime == 0f,
        "click after chain point starts next stage immediately from its beginning");
    Require(combo.Tick(0.75f, true, true) == 3 && combo.NormalizedTime == 0f,
        "new click at recovery starts exactly one continuation");
    combo.Reset();
    combo.Tick(0.01f, true, true);
    Require(combo.Tick(1.5f, true, true) == 1 && combo.NormalizedTime == 0f,
        "click arriving after recovery grace cannot be backdated to chain point");
    Require(combo.Tick(0.75f, false, true) == 0 && !combo.IsAttacking,
        "late click was not also buffered for the restarted animation");
}

static void CheckAttackRecovery()
{
    var combo = new AttackComboCore(new[]
    {
        new StepTiming(2f, 0.25f, 0.5f), new StepTiming(0.5f, 0.5f, 0.75f),
    }, 0.5f, 0.22f);
    combo.Tick(0.01f, true, true);
    combo.Tick(0.75f, false, true);
    Require(combo.IsAttacking && combo.NormalizedTime == 0.375f, "recovery timing uses current clip duration");
    Require(combo.Tick(0.25f, false, true) == 0 && !combo.IsAttacking && combo.NormalizedTime == 0.5f,
        "control unlocks at recovery instead of waiting for full animation");
    Require(combo.Tick(0.25f, true, true) == 2, "grace starts at recovery rather than full duration");
    Require(combo.Tick(0.25f, true, true) == 1, "different second-stage duration determines its own chain point");
    combo.Reset();
    combo.Tick(0.01f, true, true);
    Require(combo.Tick(1.75f, true, true) == 1, "animation tail does not extend post-recovery grace");
}

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
    player.MovementEpoch = 7;
    player.SpawnRevision = 123;
    Require(Apply(player, Move(10, 0.65f), 1100), "accepted movement");
    var copy = BsonSerializer.Deserialize<OnlineInfo>(player.ToBson());
    Require(copy.UserName == player.UserName && copy.X == player.X, "persistent data");
    Require(!copy.IsConnected && !copy.IsDirty && copy.MovementVersion == 0 && !copy.HasLastMoveSequence, "runtime data excluded");
    Require(copy.MovementEpoch == 0 && copy.SpawnRevision == 0, "correction epoch and spawn order are world-scoped");
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
    model.SetInfo(new PlayerInfo { RoleId = 42, NickName = "nickname", Hp = 90, Gold = 123, Movement = new MovementSnapshot() }, 100, 1);
    var notifications = 0;
    model.PositionCorrected += (_, _, _, _) => notifications++;
    model.CorrectPosition(100, 1, 1, new MovementSnapshot { X = 10, Y = 0, Z = 0, RotationY = 0 });
    Require(notifications == 0, "correction before world readiness");
    model.IsInWorld = true;
    model.CorrectPosition(100, 1, uint.MaxValue, new MovementSnapshot { X = 1, Y = 2, Z = 3, RotationY = 90 });
    model.CorrectPosition(100, 2, 0, new MovementSnapshot { X = 4, Y = 5, Z = 6, RotationY = 180 });
    model.CorrectPosition(100, 1, uint.MaxValue, new MovementSnapshot { X = 1, Y = 2, Z = 3, RotationY = 90 });
    model.CorrectPosition(100, 2, 0, new MovementSnapshot { X = 99, Y = 99, Z = 99, RotationY = 0 });
    model.CorrectPosition(99, 3, 1, new MovementSnapshot { X = 99 });
    Require(notifications == 2 && model.X == 4 && model.RotationY == 180, "only newer correction");
    model.Clear();
    Require(!model.HasRole && !model.IsInWorld && model.Hp == 0 && model.Gold == 0 && model.NickName == null, "logout reset");
    Require(model.WorldSessionId == 0 && !model.AcceptsWorld(100), "logout invalidates world identity");
    model.SetInfo(new PlayerInfo { RoleId = 42, Movement = new MovementSnapshot() }, 200, 1);
    model.IsInWorld = true;
    model.CorrectPosition(200, 1, 0, new MovementSnapshot { X = 1, Y = 0, Z = 0, RotationY = 0 });
    Require(notifications == 3 && model.X == 1, "correction version resets for a new login");
    model.SetInfo(new PlayerInfo { RoleId = 43, Movement = new MovementSnapshot() }, 300, 1);
    model.IsInWorld = true;
    model.CorrectPosition(200, 2, 2, new MovementSnapshot { X = 99 });
    model.CorrectPosition(300, 1, 0, new MovementSnapshot { X = 8 });
    Require(notifications == 4 && model.X == 8, "re-entering the world resets correction acknowledgement");
}

static void CheckCorrectionEpoch()
{
    var player = NewPlayer();
    Require(PlayerMovementRules.Process(player, 1, 0, Move(1, 1000).Movement, 1100)
        == PlayerMovementRules.Decision.Correction && player.MovementEpoch == 1, "rejection advances epoch");
    Require(PlayerMovementRules.Process(player, 2, 0, Move(2, 0.5f).Movement, 1200)
        == PlayerMovementRules.Decision.Correction && player.X == 0 && player.LastMoveSequence == 1
        && player.MovementEpoch == 1, "in-flight old epoch resends stable correction without moving");
    Require(PlayerMovementRules.Process(player, 3, 99, Move(3, 0.5f).Movement, 1200)
        == PlayerMovementRules.Decision.Correction && player.MovementEpoch == 1, "client cannot select a future epoch");
    Require(PlayerMovementRules.Process(player, 4, 1, Move(4, 0.5f).Movement, 1200)
        == PlayerMovementRules.Decision.Accepted && player.X == 0.5f, "acknowledged correction resumes movement");
    Require(PlayerMovementRules.Process(player, 3, 1, Move(3, 0).Movement, 1300)
        == PlayerMovementRules.Decision.Ignored, "same-epoch old sequence stays ignored");
    Require(PlayerMovementRules.Process(player, 2, 0, Move(2, 0).Movement, 1300)
        == PlayerMovementRules.Decision.Correction && player.LastCorrectionSequence == 1,
        "resending correction retains its triggering sequence after movement resumes");
    player.MovementEpoch = uint.MaxValue;
    Require(PlayerMovementRules.Process(player, 5, uint.MaxValue, Move(5, 1000).Movement, 1400)
        == PlayerMovementRules.Decision.Correction && player.MovementEpoch == 0, "epoch wraps");
    player.BindGateSession(2, 100);
    Require(player.MovementEpoch == 0, "new world resets correction epoch");
}

static void CheckSpawnOrdering()
{
    var tracker = new RemoteSpawnTracker();
    Require(tracker.Observe(42, 900, 1), "initial entry");
    Require(tracker.Observe(42, 900, 1, true), "initial departure");
    Require(tracker.Observe(42, 100, 2), "random spawn identity does not determine order");
    Require(!tracker.Observe(42, 900, 1, true) && !tracker.Observe(42, 900, 1), "old leave and baseline rejected");
    Require(tracker.Observe(42, 100, 2, true), "second departure");
    Require(!tracker.Observe(42, 900, 1) && !tracker.Observe(42, 100, 2), "two retired instances cannot resurrect");
    Require(tracker.Observe(42, 800, 3), "move may observe new spawn before entry");
    Require(!tracker.Observe(42, 100, 2) && tracker.Observe(42, 800, 3), "delayed baseline cannot replace newer move");
    Require(!tracker.Observe(42, 999, 3) && !tracker.Observe(0, 800, 3)
        && !tracker.Observe(42, 0, 4) && !tracker.Observe(42, 1, 0), "invalid identity rejected");
    tracker.Clear();
    Require(tracker.Observe(42, 900, 1), "new world has its own ordering scope");
}

static void CheckWorldReadiness()
{
    var player = NewPlayer();
    var firstWorld = player.WorldSessionId;
    Require(firstWorld != 0 && player.IsConnected && !player.IsInWorld, "enter only prepares a player");
    Require(!player.MarkWorldReady(0, 2000) && !player.MarkWorldReady(firstWorld + 1, 2000), "forged ready");
    Require(player.MarkWorldReady(firstWorld, 2000) && player.IsInWorld && player.LastMoveReceivedAt == 2000, "ready commits visibility");
    Require(player.MarkWorldReady(firstWorld, 3000) && player.LastMoveReceivedAt == 2000, "ready retry cannot refill allowance");
    player.BindGateSession(2, 100);
    Require(!player.IsInWorld && player.WorldSessionId != firstWorld, "reconnect creates a new incarnation");
    Require(!player.MatchesWorld(1, 100, firstWorld) && !player.MarkWorldReady(firstWorld, 4000), "old connection cannot act on new world");
    Require(player.MatchesWorld(2, 100, player.WorldSessionId), "new identity matches exactly");
    var copy = BsonSerializer.Deserialize<OnlineInfo>(player.ToBson());
    Require(copy.WorldSessionId == 0 && !copy.IsWorldReady, "world leases never persist to database");
}

static void CheckMovementLimiter()
{
    var gate = new PlayerSessionComponent();
    for (var i = 0; i < 5; i++) Require(gate.TryAcceptMovement(1000), "small burst");
    Require(!gate.TryAcceptMovement(1000) && !gate.TryAcceptMovement(1019), "same-time flood");
    Require(gate.TryAcceptMovement(1020), "tokens refill using elapsed time");
    var accepted = 0;
    for (var time = 1021; time <= 2020; time++) if (gate.TryAcceptMovement(time)) accepted++;
    Require(accepted == 50, "sustained rate is fifty per second");
    gate.WorldSessionId = 42;
    gate.IsWorldReady = true;
    gate.ResetWorldState();
    Require(gate.WorldSessionId == 0 && !gate.IsWorldReady && gate.TryAcceptMovement(3000), "pooled session reset");
}

static void CheckIdlePersistence()
{
    var player = NewPlayer();
    Require(PlayerMovementRules.TryApply(player, 1, new MovementSnapshot(), 1100), "idle heartbeat accepted");
    Require(!player.IsDirty && player.HasLastMoveSequence, "runtime-only heartbeat is not a database write");
    Require(PlayerMovementRules.TryApply(player, 2, new MovementSnapshot { RotationY = 45 }, 1200) && player.IsDirty, "rotation persists");
}

static void CheckSnapshotTimeline()
{
    var timeline = new SnapshotTimeline<float>(0.15, 4);
    Require(!timeline.TrySample(0, out _, out _, out _), "empty timeline");
    const long origin = 9000000000000;
    timeline.Add(origin, 10, 0);
    timeline.Add(origin + 100, 10.1, 1);
    timeline.Add(origin + 200, 10.25, 2);
    Require(timeline.TrySample(10.3, out var from, out var to, out var fraction)
        && from == 1 && to == 2 && Math.Abs(fraction - 0.5f) < 0.001, "server-time interpolation survives arrival jitter");
    Require(!timeline.Add(origin + 150, 10.4, 99), "old server sample rejected");
    timeline.TrySample(10.2, out _, out _, out var backwards);
    Require(backwards >= fraction, "local clock cannot rewind playback");
    timeline.Add(origin + 200, 10.4, 3);
    timeline.TrySample(20, out _, out to, out fraction);
    Require(to == 3 && fraction == 1, "same timestamp replaces newest and missing packets freeze");
    for (var i = 3; i < 1000; i++) timeline.Add(origin + i * 100, 10 + i * 0.1, i);
    Require(timeline.Count == 4, "bounded memory even before rendering");
    timeline.Clear();
    timeline.Add(1, 100, 7);
    timeline.TrySample(100, out from, out to, out _);
    Require(from == 7 && to == 7 && timeline.Count == 1, "new spawn resets its clock and samples");
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
    var request = Serializer.DeepClone(new C2G_PlayerMove { WorldSessionId = long.MaxValue - 7, Sequence = uint.MaxValue, MovementEpoch = 7, Movement = movement });
    Require(request.Sequence == uint.MaxValue && request.MovementEpoch == 7 && request.Movement.State == PlayerMotionState.Run
        && request.Movement.X == 1 && request.WorldSessionId == long.MaxValue - 7, "movement round trip");
    var entered = Serializer.DeepClone(new Game2G_PlayerEntered { GateSessionRuntimeId = 123, WorldSessionId = 99,
        Player = new PlayerPresence { RoleId = 42, NickName = "nickname", Version = 8, SpawnId = long.MaxValue - 19,
            ServerTimeMs = 123456789, SpawnRevision = ulong.MaxValue, Movement = movement } });
    Require(entered.Player.Version == 8 && entered.Player.Movement.Speed == 6.5f
        && entered.GateSessionRuntimeId == 123 && entered.WorldSessionId == 99
        && entered.Player.SpawnId == long.MaxValue - 19 && entered.Player.ServerTimeMs == 123456789 && entered.Player.SpawnRevision == ulong.MaxValue, "inner shared DTO round trip");
    var response = Serializer.DeepClone(new Game2G_EnterGameResponse { Result = ResultCode.Success,
        ErrorCode = 17, Info = new PlayerInfo { RoleId = 42, Gold = 100, Movement = movement } });
    Require(response.Result == ResultCode.Success && response.ErrorCode == 17
        && response.Info.Gold == 100, "business result and framework error remain independent");
    var correction = Serializer.DeepClone(new G2C_PlayerPositionCorrection { WorldSessionId = 77, AckSequence = 9, MovementEpoch = 8, Movement = movement });
    Require(correction.WorldSessionId == 77 && correction.MovementEpoch == 8 && correction.AckSequence == 9 && correction.Movement.RotationY == 90, "correction round trip");
    var baseline = new G2C_WorldReadyResponse { Result = ResultCode.Success };
    baseline.Players.Add(entered.Player);
    var ready = Serializer.DeepClone(baseline);
    Require(ready.Players.Count == 1 && ready.Players[0].SpawnId == entered.Player.SpawnId, "ready baseline round trip");
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
        nameof(NetworkProtocolHelper.C2G_WorldReadyRequest),
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
