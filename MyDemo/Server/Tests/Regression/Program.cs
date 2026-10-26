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
    ("Shared combat IDs and timings match the generated protocol", CheckActionContract),
    ("Client combat input, interruption and cooldown stay deterministic", ClientCombatTests.Run),
    ("Combat commands enforce ordered chains without losing sequence wraparound", CheckActionOrder),
    ("Skills and slide respect action locks, cooldowns and precise cancellation", CheckActionCooldown),
    ("Action baselines retain cast origin and survive protobuf serialization", CheckActionSnapshot),
    ("Action limiter is independent of movement and resets with its world", CheckActionLimiter),
    ("Correction epochs reject in-flight pre-correction movement", CheckCorrectionEpoch),
    ("Spawn ordering rejects retired instances across repeated reconnects", CheckSpawnOrdering),
    ("Role responses contain independent DTOs", CheckRoleMapping),
    ("Database snapshots exclude session and runtime movement fields", CheckPersistence),
    ("AccountUser naming retains existing MongoDB accounts and roles", CheckAccountUserStorage),
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

static void CheckActionContract()
{
    foreach (var kind in Enum.GetValues<PlayerActionKind>())
    {
        var id = (PlayerActionId)kind;
        Require(PlayerActionRules.IsDefined(id), "every wire action has shared timing");
        var timing = PlayerActionRules.Get(id);
        Require(timing.DurationMs == (int)Math.Round(timing.Duration * 1000d, MidpointRounding.AwayFromZero), "wire milliseconds");
        Require(id == PlayerActionId.None || timing.Duration > 0 && timing.RecoverAt > 0 && timing.RecoverAt <= 1,
            "valid lock duration");
    }
    Require((int)PlayerActionId.Skill01 == 11 && (int)PlayerActionId.Skill04 == 14
        && (int)PlayerActionId.Slide == 20 && (int)ProtocolRevision.Current == 4, "stable action IDs and revision");
    Require(PlayerActionRules.SlideSpeed <= 8f && PlayerActionRules.Get(PlayerActionId.Slide).DurationMs == 650,
        "slide fits existing authoritative movement budget");
    Require(!PlayerActionRules.IsDefined((PlayerActionId)10) && !PlayerActionRules.IsDefined((PlayerActionId)999),
        "unknown wire actions are not configuration lookups");
}

static void CheckActionOrder()
{
    var state = new PlayerActionState();
    Require(PlayerActionRules.Process(state, 1, PlayerActionId.Attack01, -90, 0, 1000) == PlayerActionRules.Decision.Accepted
        && state.Version == 1 && state.RotationY == 270, "first action accepted");
    var firstChain = PlayerActionRules.Get(PlayerActionId.Attack01).ChainAtMs;
    Require(PlayerActionRules.Process(state, 2, PlayerActionId.Attack02, 0, 0,
        1000 + firstChain - PlayerActionRules.TimingToleranceMs - 1) == PlayerActionRules.Decision.Busy, "no early chain exploit");
    Require(PlayerActionRules.Process(state, 3, PlayerActionId.Attack03, 0, 0, 1000 + firstChain)
        == PlayerActionRules.Decision.InvalidAction && state.Version == 1, "cannot skip an attack stage");
    Require(PlayerActionRules.Process(state, 4, PlayerActionId.Attack02, 0, 0, 1000 + firstChain)
        == PlayerActionRules.Decision.Accepted, "chain two at its real chain point");
    var thirdAt = 1000 + firstChain + PlayerActionRules.Get(PlayerActionId.Attack02).ChainAtMs;
    Require(PlayerActionRules.Process(state, 5, PlayerActionId.Attack03, 0, 0, thirdAt)
        == PlayerActionRules.Decision.Accepted, "chain three");
    var cycleAt = thirdAt + PlayerActionRules.Get(PlayerActionId.Attack03).ChainAtMs;
    Require(PlayerActionRules.Process(state, 6, PlayerActionId.Attack01, 0, 0, cycleAt)
        == PlayerActionRules.Decision.Accepted, "cycles to one");
    Require(PlayerActionRules.Process(state, 7, PlayerActionId.Attack02, 0, 0, cycleAt + 3000)
        == PlayerActionRules.Decision.InvalidAction, "expired combo requires first stage");
    Require(PlayerActionRules.Process(state, 8, PlayerActionId.Attack01, 0, 0, cycleAt + 3000)
        == PlayerActionRules.Decision.Accepted, "expired combo restarts");
    Require(PlayerActionRules.Process(state, 7, PlayerActionId.Skill01, 0, 0, cycleAt + 5000)
        == PlayerActionRules.Decision.Ignored && state.Sequence == 8, "late old command cannot replace a new action");

    state.Reset();
    Require(PlayerActionRules.Process(state, uint.MaxValue, PlayerActionId.Attack01, 0, 0, 1000)
        == PlayerActionRules.Decision.Accepted, "first sequence may be maximum");
    Require(PlayerActionRules.Process(state, 0, PlayerActionId.Attack02, 0, 0, 1000 + firstChain)
        == PlayerActionRules.Decision.Accepted && state.Version == 2, "action sequence wraps independently of movement");
    Require(PlayerActionRules.Process(state, uint.MaxValue, PlayerActionId.Skill01, 0, 0, 5000)
        == PlayerActionRules.Decision.Ignored, "pre-wrap command stays old");
}

static void CheckActionCooldown()
{
    var state = new PlayerActionState();
    Require(PlayerActionRules.Process(state, 1, PlayerActionId.Skill04, 0, 0, 1000)
        == PlayerActionRules.Decision.Accepted, "skill starts");
    Require(PlayerActionRules.Process(state, 2, PlayerActionId.Slide, 0, 0, 1200)
        == PlayerActionRules.Decision.Busy, "F cannot cancel locked skill");
    Require(PlayerActionRules.Process(state, 3, PlayerActionId.Skill04, 0, 0, 2300)
        == PlayerActionRules.Decision.Cooldown, "recovery does not clear skill cooldown");
    Require(PlayerActionRules.Process(state, 4, PlayerActionId.Skill01, 0, 0, 2300)
        == PlayerActionRules.Decision.Accepted, "another ready skill starts after recovery");
    Require(PlayerActionRules.Process(state, 5, PlayerActionId.None, 0, 1, 2310)
        == PlayerActionRules.Decision.Ignored && state.Kind == PlayerActionId.Skill01, "old cancel cannot stop newer action");
    Require(PlayerActionRules.Process(state, 6, PlayerActionId.None, 0, 4, 2320)
        == PlayerActionRules.Decision.Accepted && state.Kind == PlayerActionId.None, "matching cancel stops exact action");
    Require(PlayerActionRules.Process(state, 7, PlayerActionId.Skill01, 0, 0, 2330)
        == PlayerActionRules.Decision.Cooldown, "cancellation retains consumed cooldown");
    Require(PlayerActionRules.Process(state, 8, PlayerActionId.Slide, 0, 0, 2500)
        == PlayerActionRules.Decision.Accepted, "ready slide starts");
    Require(PlayerActionRules.Process(state, 9, PlayerActionId.Slide, 0, 0, 3200)
        == PlayerActionRules.Decision.Cooldown, "held or repeated F cannot extend one slide");
    Require(PlayerActionRules.Process(state, 10, (PlayerActionId)999, 0, 0, 3700)
        == PlayerActionRules.Decision.InvalidAction, "malformed action rejected without lookup exception");
    Require(PlayerActionRules.Process(state, 11, PlayerActionId.Slide, float.NaN, 0, 3700)
        == PlayerActionRules.Decision.InvalidAction, "nonfinite facing rejected");
    Require(PlayerActionRules.Process(state, 12, PlayerActionId.Slide, 0, 0, 3700)
        == PlayerActionRules.Decision.Accepted, "slide cooldown ends at shared deadline");
    state.Reset();
    Require(PlayerActionRules.Process(state, 1, PlayerActionId.Skill04, 0, 0, 4000)
        == PlayerActionRules.Decision.Accepted && state.Version == 1, "new world clears old action identity");
}

static void CheckActionSnapshot()
{
    var player = NewPlayer();
    PlayerActionRules.Process(player.Actions, 8, PlayerActionId.Skill04, 90, 0, 1000);
    player.Actions.OriginX = 2;
    player.Actions.OriginY = 3;
    player.Actions.OriginZ = 4;
    var presence = PlayerMessageMapper.ToPresence(player);
    var cloned = Serializer.DeepClone(new G2C_PlayerAction
    {
        RoleId = player.Id, WorldSessionId = 77, SpawnId = 88, SpawnRevision = 99,
        Action = presence.Action, ServerTimeMs = 1100,
    });
    Require(cloned.Action.Kind == PlayerActionKind.Skill04 && cloned.Action.Sequence == 8
        && cloned.Action.DurationMs == 1200 && cloned.Action.StartedAtServerTimeMs == 1000
        && cloned.Action.Version == 1 && cloned.Action.RotationY == 90, "action envelope preserves timing and identity");
    Require(cloned.Action.OriginX == 2 && cloned.Action.OriginY == 3 && cloned.Action.OriginZ == 4,
        "late playback retains cast position");
    presence.Action.OriginX = 100;
    Require(player.Actions.OriginX == 2 && cloned.Action.OriginX == 2, "DTO does not retain mutable entity data");
    var response = Serializer.DeepClone(new G2C_PlayerActionResponse
        { Sequence = 9, Result = ResultCode.Busy, Action = cloned.Action, ServerTimeMs = 1200 });
    Require(response.Sequence == 9 && response.Action.Sequence == 8 && response.Result == ResultCode.Busy,
        "rejection keeps request and accepted action sequences distinct");
    var copy = BsonSerializer.Deserialize<OnlineInfo>(player.ToBson());
    Require(copy.Actions.Kind == PlayerActionId.None && copy.Actions.Version == 0
        && !copy.Actions.HasRequestSequence, "runtime action state excluded from persistence");
}

static void CheckActionLimiter()
{
    var player = NewPlayer();
    for (var index = 0; index < 4; index++) Require(player.TryAcceptAction(1000), "small command burst");
    Require(!player.TryAcceptAction(1000) && !player.TryAcceptAction(1099), "unbounded RPC burst rejected");
    Require(player.TryAcceptAction(1100) && player.TryAcceptMovement(1100), "independent budgets replenish by time");
    player.BindGateSession(2, 100);
    Require(player.TryAcceptAction(2000), "new world resets action limiter");
}

// 对外响应必须有完整数据；修改 DTO 不能修改在线实体。
static void CheckRoleMapping()
{
    var player = BsonSerializer.Deserialize<OnlineInfo>("{ '_id': 42 }");
    player.AccountUser = "account";
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
    player.AccountUser = "account";
    player.MovementEpoch = 7;
    player.SpawnRevision = 123;
    Require(Apply(player, Move(10, 0.65f), 1100), "accepted movement");
    var copy = BsonSerializer.Deserialize<OnlineInfo>(player.ToBson());
    Require(copy.AccountUser == player.AccountUser && copy.X == player.X, "persistent data");
    Require(!copy.IsConnected && !copy.IsDirty && copy.MovementVersion == 0 && !copy.HasLastMoveSequence, "runtime data excluded");
    Require(copy.MovementEpoch == 0 && copy.SpawnRevision == 0, "correction epoch and spawn order are world-scoped");
}

// 改代码中的账号命名后，仍按原有 MongoDB 字段读写，无须迁移玩家数据。
static void CheckAccountUserStorage()
{
    var account = BsonSerializer.Deserialize<Account>(new BsonDocument("Username", "existing-account"));
    Require(account.AccountUser == "existing-account", "existing account is readable");
    var accountDocument = account.ToBsonDocument();
    Require(accountDocument["Username"] == "existing-account" && !accountDocument.Contains("AccountUser"),
        "account writes retain the original storage field");
    Require(BsonClassMap.LookupClassMap(typeof(Account)).GetMemberMap(nameof(Account.AccountUser)).ElementName == "Username",
        "account query expressions map to the original field");

    var player = BsonSerializer.Deserialize<OnlineInfo>(new BsonDocument
    {
        { "UserName", "existing-account" }, { "NickName", "existing-role" },
    });
    Require(player.AccountUser == "existing-account" && player.NickName == "existing-role", "existing role is readable");
    var playerDocument = player.ToBsonDocument();
    Require(playerDocument["UserName"] == "existing-account" && !playerDocument.Contains("AccountUser"),
        "role writes retain the original storage field");
    Require(BsonClassMap.LookupClassMap(typeof(OnlineInfo)).GetMemberMap(nameof(OnlineInfo.AccountUser)).ElementName == "UserName",
        "role query expressions map to the original field");
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
        new C2G_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { X = float.NaN } },
        new C2G_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { Y = float.PositiveInfinity } },
        new C2G_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { Z = float.NegativeInfinity } },
        new C2G_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { RotationY = float.NaN } },
        new C2G_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { Speed = float.NaN } },
        new C2G_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { Speed = -1 } },
        new C2G_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { Speed = 100 } },
        new C2G_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { State = (PlayerMotionState)999 } },
        new C2G_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { Y = 100 } },
        new C2G_PlayerMove { Sequence = 1 },
        new C2G_PlayerMove { Sequence = 1, Movement = new MovementSnapshot { State = (PlayerMotionState)(-1) } },
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
        Require(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims
            .Any(claim => claim.Type == "AccountUser" && claim.Value == "account"), "token carries AccountUser identity");
        Require(gate.TryValidateToken(token, 1002, out var account) && account == "account", "valid token");
        Require(!gate.TryValidateToken(token, 1003, out _), "wrong account");
        Require(!gate.TryValidateToken("invalid", 1002, out _), "malformed token");
        Require(!gate.TryValidateToken("", 1002, out _), "empty token");
        var expired = new JwtSecurityToken(issuer: "Fzy", audience: "Fzy",
            claims: new[] { new Claim("AccountUser", "account"), new Claim("GateSceneId", "1002") },
            notBefore: DateTime.UtcNow.AddMinutes(-5), expires: DateTime.UtcNow.AddMinutes(-2),
            signingCredentials: authentication.SigningCredentials);
        Require(!gate.TryValidateToken(new JwtSecurityTokenHandler().WriteToken(expired), 1002, out _), "expired token");
        using var unrelatedKey = RSA.Create(2048);
        var forged = new JwtSecurityToken(issuer: "Fzy", audience: "Fzy",
            claims: new[] { new Claim("AccountUser", "account"), new Claim("GateSceneId", "1002") }, expires: DateTime.UtcNow.AddMinutes(5),
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
    var player = NewPlayer();
    for (var i = 0; i < 5; i++) Require(player.TryAcceptMovement(1000), "small burst");
    Require(!player.TryAcceptMovement(1000) && !player.TryAcceptMovement(1019), "same-time flood");
    Require(player.TryAcceptMovement(1020), "tokens refill using elapsed time");
    var accepted = 0;
    for (var time = 1021; time <= 2020; time++) if (player.TryAcceptMovement(time)) accepted++;
    Require(accepted == 50, "sustained rate is fifty per second");
    player.WorldSessionId = 42;
    player.IsWorldReady = true;
    player.BindGateSession(2, 100);
    Require(player.WorldSessionId != 42 && !player.IsWorldReady && player.TryAcceptMovement(3000), "new player incarnation resets limiter");
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
    Require(typeof(PlayerPresence).GetProperty("AccountUser") == null
        && typeof(PlayerInfo).GetProperty("AccountUser") == null, "no account exposure");
    Require(typeof(G2C_CreateRoleResponse).GetProperty("Info") == null, "create result only");
    Require(typeof(G2Game_CreateRoleRequest).GetProperty("GateSessionRuntimeId") == null, "create does not bind session");
    Require(typeof(C2G_LoginRequest).GetProperty("AccountUser") == null, "identity comes from signed token");
    Require(typeof(Game2G_EnterGameResponse).GetProperty("Info")?.PropertyType == typeof(PlayerInfo), "shared DTO");
    Require(typeof(G2C_PlayerPositionCorrection).GetProperty("AckSequence") != null
        && typeof(G2C_PlayerMove).GetProperty("Version") != null, "separate correction and broadcast");
}

// 使用生成的序列化器而非模拟 DTO，检查字段编号和嵌套数据。
static void CheckProtocolSerialization()
{
    var login = Serializer.DeepClone(new C2A_LoginRequest { AccountUser = "account", Password = "password" });
    Require(login.AccountUser == "account" && login.Password == "password", "login account round trip");
    var enterRequest = Serializer.DeepClone(new G2Game_EnterGameRequest
    {
        AccountUser = "account", GateSessionRuntimeId = 17, GateSceneAddress = 23,
    });
    Require(enterRequest.AccountUser == "account" && enterRequest.GateSessionRuntimeId == 17
        && enterRequest.GateSceneAddress == 23, "internal account identity round trip");
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
    Require(session.Reused.AccountUser == "new-request" && session.Reused.IsPool(), "await must not release reused request");
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
        nameof(NetworkProtocolHelper.C2G_PlayerActionRequest),
    };
    var methods = typeof(NetworkProtocolHelper).GetMethods(
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly);
    Require(methods.Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal)
        .SequenceEqual(expected.OrderBy(name => name, StringComparer.Ordinal)), "only required client sends");
    foreach (var method in methods)
    {
        var parameters = method.GetParameters();
        Require(parameters[0].ParameterType == typeof(Fantasy.Network.Session), "session extension");
        Require(parameters.Skip(1).All(parameter => parameter.ParameterType.IsPrimitive || parameter.ParameterType.IsEnum
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

static C2G_PlayerMove Move(uint sequence, float x) => new()
{
    Sequence = sequence, Movement = new MovementSnapshot { X = x, State = PlayerMotionState.Run, Speed = 6.5f },
};

// 测试通过同一入口提交协议采样，生产规则不依赖整条路由消息。
static bool Apply(OnlineInfo player, C2G_PlayerMove request, long now)
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
        Reused.AccountUser = "new-request";
        return completion;
    }

    public void Complete() => completion.SetResult(new A2C_LoginResponse { Result = ResultCode.Success });
    private static void RequireReuse(bool reused)
    {
        if (!reused) throw new InvalidOperationException("Test did not reuse the pooled request.");
    }
}
