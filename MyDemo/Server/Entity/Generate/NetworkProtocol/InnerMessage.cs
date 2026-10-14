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
            UserName = default;
            GateSessionRuntimeId = default;
            GateSceneAddress = default;
            MessageObjectPool<G2Game_EnterGameRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_EnterGameRequest; }
        [ProtoIgnore]
        public Game2G_EnterGameResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string UserName { get; set; }
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
            MessageObjectPool<Game2G_EnterGameResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_EnterGameResponse; }
        [ProtoMember(3)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public ResultCode Result { get; set; }
        [ProtoMember(2)]
        public PlayerInfo Info { get; set; }
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
            UserName = default;
            NickName = default;
            MessageObjectPool<G2Game_CreateRoleRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_CreateRoleRequest; }
        [ProtoIgnore]
        public Game2G_CreateRoleResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string UserName { get; set; }
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
    /// 经过 Gate 限频和身份校验的移动采样
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Game_PlayerMove : AMessage, IAddressMessage
    {
        public static G2Game_PlayerMove Create(bool autoReturn = true)
        {
            var g2Game_PlayerMove = MessageObjectPool<G2Game_PlayerMove>.Rent();
            g2Game_PlayerMove.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                g2Game_PlayerMove.SetIsPool(false);
            }

            return g2Game_PlayerMove;
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
            UserName = default;
            GateSessionRuntimeId = default;
            GateSceneAddress = default;
            Sequence = default;
            Movement = default;
            MessageObjectPool<G2Game_PlayerMove>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_PlayerMove; }
        [ProtoMember(1)]
        public string UserName { get; set; }
        [ProtoMember(2)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(3)]
        public long GateSceneAddress { get; set; }
        [ProtoMember(4)]
        public uint Sequence { get; set; }
        [ProtoMember(5)]
        public MovementSnapshot Movement { get; set; }
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
            MessageObjectPool<Game2G_PlayerPositionCorrection>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_PlayerPositionCorrection; }
        [ProtoMember(1)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(2)]
        public uint AckSequence { get; set; }
        [ProtoMember(3)]
        public MovementSnapshot Movement { get; set; }
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
            MessageObjectPool<Game2G_PlayerEntered>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_PlayerEntered; }
        [ProtoMember(1)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(2)]
        public PlayerPresence Player { get; set; }
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
            UserName = default;
            GateSessionRuntimeId = default;
            GateSceneAddress = default;
            MessageObjectPool<G2Game_PlayerLeft>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_PlayerLeft; }
        [ProtoMember(1)]
        public string UserName { get; set; }
        [ProtoMember(2)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(3)]
        public long GateSceneAddress { get; set; }
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
            MessageObjectPool<Game2G_PlayerLeft>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_PlayerLeft; }
        [ProtoMember(1)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(2)]
        public long RoleId { get; set; }
    }
}