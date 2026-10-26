using LightProto;
using MemoryPack;
using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PreferConcreteValueOverDefault
// ReSharper disable RedundantNameQualifier
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CheckNamespace
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable RedundantUsingDirective
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
namespace Fantasy
{
    /// <summary>
    /// Gate 提供可信账号和连接路由，客户端不能指定这些字段
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Game_EnterGameRequest : AMessage, IAddressRequest
    {
        public static G2Game_EnterGameRequest Create(bool autoReturn = true)
        {
            var g2Game_EnterGameRequest = MessageObjectPool<G2Game_EnterGameRequest>.Rent();
            g2Game_EnterGameRequest.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                g2Game_EnterGameRequest.SetIsPool(false);
            }

            return g2Game_EnterGameRequest;
        }

        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return;
            AccountUser = default;
            GateSessionRuntimeId = default;
            GateSceneAddress = default;
            MessageObjectPool<G2Game_EnterGameRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_EnterGameRequest; }
        [ProtoIgnore]
        public Game2G_EnterGameResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string AccountUser { get; set; }
        [ProtoMember(2)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(3)]
        public long GateSceneAddress { get; set; }
    }
    /// <summary>
    /// 直接复用外网 DTO；不传数据库实体
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class Game2G_EnterGameResponse : AMessage, IAddressResponse
    {
        public static Game2G_EnterGameResponse Create(bool autoReturn = true)
        {
            var game2G_EnterGameResponse = MessageObjectPool<Game2G_EnterGameResponse>.Rent();
            game2G_EnterGameResponse.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                game2G_EnterGameResponse.SetIsPool(false);
            }

            return game2G_EnterGameResponse;
        }

        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return;
            ErrorCode = 0;
            Result = default;
            Info = default;
            WorldSessionId = default;
            WorldId = default;
            PlayerAddress = default;
            MessageObjectPool<Game2G_EnterGameResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_EnterGameResponse; }
        [ProtoMember(6)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public ResultCode Result { get; set; }
        [ProtoMember(2)]
        public PlayerInfo Info { get; set; }
        [ProtoMember(3)]
        public long WorldSessionId { get; set; }
        [ProtoMember(4)]
        public int WorldId { get; set; }
        [ProtoMember(5)]
        public long PlayerAddress { get; set; }
    }
    /// <summary>
    /// 只有与 Enter 返回的世界会话匹配，才正式加入公共世界
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Game_WorldReadyRequest : AMessage, IAddressRequest
    {
        public static G2Game_WorldReadyRequest Create(bool autoReturn = true)
        {
            var g2Game_WorldReadyRequest = MessageObjectPool<G2Game_WorldReadyRequest>.Rent();
            g2Game_WorldReadyRequest.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                g2Game_WorldReadyRequest.SetIsPool(false);
            }

            return g2Game_WorldReadyRequest;
        }

        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return;
            AccountUser = default;
            GateSessionRuntimeId = default;
            GateSceneAddress = default;
            WorldSessionId = default;
            MessageObjectPool<G2Game_WorldReadyRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_WorldReadyRequest; }
        [ProtoIgnore]
        public Game2G_WorldReadyResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string AccountUser { get; set; }
        [ProtoMember(2)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(3)]
        public long GateSceneAddress { get; set; }
        [ProtoMember(4)]
        public long WorldSessionId { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class Game2G_WorldReadyResponse : AMessage, IAddressResponse
    {
        public static Game2G_WorldReadyResponse Create(bool autoReturn = true)
        {
            var game2G_WorldReadyResponse = MessageObjectPool<Game2G_WorldReadyResponse>.Rent();
            game2G_WorldReadyResponse.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                game2G_WorldReadyResponse.SetIsPool(false);
            }

            return game2G_WorldReadyResponse;
        }

        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return;
            ErrorCode = 0;
            Result = default;
            Players.Clear();
            MessageObjectPool<Game2G_WorldReadyResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_WorldReadyResponse; }
        [ProtoMember(3)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public ResultCode Result { get; set; }
        [ProtoMember(2)]
        public List<PlayerPresence> Players { get; set; } = new List<PlayerPresence>();
    }
    /// <summary>
    /// 创建数据不绑定在线连接，绑定统一由 EnterGame 完成
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Game_CreateRoleRequest : AMessage, IAddressRequest
    {
        public static G2Game_CreateRoleRequest Create(bool autoReturn = true)
        {
            var g2Game_CreateRoleRequest = MessageObjectPool<G2Game_CreateRoleRequest>.Rent();
            g2Game_CreateRoleRequest.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                g2Game_CreateRoleRequest.SetIsPool(false);
            }

            return g2Game_CreateRoleRequest;
        }

        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return;
            AccountUser = default;
            NickName = default;
            MessageObjectPool<G2Game_CreateRoleRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_CreateRoleRequest; }
        [ProtoIgnore]
        public Game2G_CreateRoleResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string AccountUser { get; set; }
        [ProtoMember(2)]
        public string NickName { get; set; }
    }
    /// <summary>
    /// 创建结果，不重复返回角色数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class Game2G_CreateRoleResponse : AMessage, IAddressResponse
    {
        public static Game2G_CreateRoleResponse Create(bool autoReturn = true)
        {
            var game2G_CreateRoleResponse = MessageObjectPool<Game2G_CreateRoleResponse>.Rent();
            game2G_CreateRoleResponse.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                game2G_CreateRoleResponse.SetIsPool(false);
            }

            return game2G_CreateRoleResponse;
        }

        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return;
            ErrorCode = 0;
            Result = default;
            MessageObjectPool<Game2G_CreateRoleResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_CreateRoleResponse; }
        [ProtoMember(2)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public ResultCode Result { get; set; }
    }
    /// <summary>
    /// 定向转发其他玩家的移动版本
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class Game2G_PlayerMove : AMessage, IAddressMessage
    {
        public static Game2G_PlayerMove Create(bool autoReturn = true)
        {
            var game2G_PlayerMove = MessageObjectPool<Game2G_PlayerMove>.Rent();
            game2G_PlayerMove.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                game2G_PlayerMove.SetIsPool(false);
            }

            return game2G_PlayerMove;
        }

        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return;
            GateSessionRuntimeId = default;
            RoleId = default;
            Version = default;
            Movement = default;
            WorldSessionId = default;
            SpawnId = default;
            ServerTimeMs = default;
            SpawnRevision = default;
            MessageObjectPool<Game2G_PlayerMove>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_PlayerMove; }
        [ProtoMember(1)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(2)]
        public long RoleId { get; set; }
        [ProtoMember(3)]
        public uint Version { get; set; }
        [ProtoMember(4)]
        public MovementSnapshot Movement { get; set; }
        [ProtoMember(5)]
        public long WorldSessionId { get; set; }
        [ProtoMember(6)]
        public long SpawnId { get; set; }
        [ProtoMember(7)]
        public long ServerTimeMs { get; set; }
        [ProtoMember(8)]
        public ulong SpawnRevision { get; set; }
    }
    /// <summary>
    /// 定向回正被拒绝的移动
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class Game2G_PlayerPositionCorrection : AMessage, IAddressMessage
    {
        public static Game2G_PlayerPositionCorrection Create(bool autoReturn = true)
        {
            var game2G_PlayerPositionCorrection = MessageObjectPool<Game2G_PlayerPositionCorrection>.Rent();
            game2G_PlayerPositionCorrection.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                game2G_PlayerPositionCorrection.SetIsPool(false);
            }

            return game2G_PlayerPositionCorrection;
        }

        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return;
            GateSessionRuntimeId = default;
            AckSequence = default;
            Movement = default;
            WorldSessionId = default;
            MovementEpoch = default;
            MessageObjectPool<Game2G_PlayerPositionCorrection>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_PlayerPositionCorrection; }
        [ProtoMember(1)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(2)]
        public uint AckSequence { get; set; }
        [ProtoMember(3)]
        public MovementSnapshot Movement { get; set; }
        [ProtoMember(4)]
        public long WorldSessionId { get; set; }
        [ProtoMember(5)]
        public uint MovementEpoch { get; set; }
    }
    /// <summary>
    /// 定向发送公开角色数据
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class Game2G_PlayerEntered : AMessage, IAddressMessage
    {
        public static Game2G_PlayerEntered Create(bool autoReturn = true)
        {
            var game2G_PlayerEntered = MessageObjectPool<Game2G_PlayerEntered>.Rent();
            game2G_PlayerEntered.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                game2G_PlayerEntered.SetIsPool(false);
            }

            return game2G_PlayerEntered;
        }

        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return;
            GateSessionRuntimeId = default;
            Player = default;
            WorldSessionId = default;
            MessageObjectPool<Game2G_PlayerEntered>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_PlayerEntered; }
        [ProtoMember(1)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(2)]
        public PlayerPresence Player { get; set; }
        [ProtoMember(3)]
        public long WorldSessionId { get; set; }
    }
    /// <summary>
    /// 旧连接下线必须与原路由匹配，不能清除新连接
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Game_PlayerLeft : AMessage, IAddressMessage
    {
        public static G2Game_PlayerLeft Create(bool autoReturn = true)
        {
            var g2Game_PlayerLeft = MessageObjectPool<G2Game_PlayerLeft>.Rent();
            g2Game_PlayerLeft.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                g2Game_PlayerLeft.SetIsPool(false);
            }

            return g2Game_PlayerLeft;
        }

        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return;
            AccountUser = default;
            GateSessionRuntimeId = default;
            GateSceneAddress = default;
            WorldSessionId = default;
            MessageObjectPool<G2Game_PlayerLeft>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_PlayerLeft; }
        [ProtoMember(1)]
        public string AccountUser { get; set; }
        [ProtoMember(2)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(3)]
        public long GateSceneAddress { get; set; }
        [ProtoMember(4)]
        public long WorldSessionId { get; set; }
    }
    /// <summary>
    /// 定向发送离开通知
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class Game2G_PlayerLeft : AMessage, IAddressMessage
    {
        public static Game2G_PlayerLeft Create(bool autoReturn = true)
        {
            var game2G_PlayerLeft = MessageObjectPool<Game2G_PlayerLeft>.Rent();
            game2G_PlayerLeft.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                game2G_PlayerLeft.SetIsPool(false);
            }

            return game2G_PlayerLeft;
        }

        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return;
            GateSessionRuntimeId = default;
            RoleId = default;
            WorldSessionId = default;
            SpawnId = default;
            SpawnRevision = default;
            MessageObjectPool<Game2G_PlayerLeft>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_PlayerLeft; }
        [ProtoMember(1)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(2)]
        public long RoleId { get; set; }
        [ProtoMember(3)]
        public long WorldSessionId { get; set; }
        [ProtoMember(4)]
        public long SpawnId { get; set; }
        [ProtoMember(5)]
        public ulong SpawnRevision { get; set; }
    }
    /// <summary>
    /// 定向转发一次确认动作，不与移动快照合并
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class Game2G_PlayerAction : AMessage, IAddressMessage
    {
        public static Game2G_PlayerAction Create(bool autoReturn = true)
        {
            var game2G_PlayerAction = MessageObjectPool<Game2G_PlayerAction>.Rent();
            game2G_PlayerAction.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                game2G_PlayerAction.SetIsPool(false);
            }

            return game2G_PlayerAction;
        }

        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return;
            GateSessionRuntimeId = default;
            WorldSessionId = default;
            RoleId = default;
            SpawnId = default;
            SpawnRevision = default;
            Action = default;
            ServerTimeMs = default;
            MessageObjectPool<Game2G_PlayerAction>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_PlayerAction; }
        [ProtoMember(1)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(2)]
        public long WorldSessionId { get; set; }
        [ProtoMember(3)]
        public long RoleId { get; set; }
        [ProtoMember(4)]
        public long SpawnId { get; set; }
        [ProtoMember(5)]
        public ulong SpawnRevision { get; set; }
        [ProtoMember(6)]
        public PlayerActionSnapshot Action { get; set; }
        [ProtoMember(7)]
        public long ServerTimeMs { get; set; }
    }
}