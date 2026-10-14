using LightProto;
using System;
using MemoryPack;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
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
namespace Fantasy
{
    /// <summary>
    /// 位置为世界坐标；RotationY 为角度，Speed 为实际水平速度（米/秒）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class MovementSnapshot : AMessage, IDisposable
    {
        public static MovementSnapshot Create(bool autoReturn = true)
        {
            var movementSnapshot = MessageObjectPool<MovementSnapshot>.Rent();
            movementSnapshot.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                movementSnapshot.SetIsPool(false);
            }

            return movementSnapshot;
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
            X = default;
            Y = default;
            Z = default;
            RotationY = default;
            State = default;
            Speed = default;
            MessageObjectPool<MovementSnapshot>.Return(this);
        }
        [ProtoMember(1)]
        public float X { get; set; }
        [ProtoMember(2)]
        public float Y { get; set; }
        [ProtoMember(3)]
        public float Z { get; set; }
        [ProtoMember(4)]
        public float RotationY { get; set; }
        [ProtoMember(5)]
        public PlayerMotionState State { get; set; }
        [ProtoMember(6)]
        public float Speed { get; set; }
    }
    /// <summary>
    /// 只发给角色本人的数据，不包含登录账号
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class PlayerInfo : AMessage, IDisposable
    {
        public static PlayerInfo Create(bool autoReturn = true)
        {
            var playerInfo = MessageObjectPool<PlayerInfo>.Rent();
            playerInfo.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                playerInfo.SetIsPool(false);
            }

            return playerInfo;
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
            RoleId = default;
            NickName = default;
            Hp = default;
            MaxHp = default;
            Mp = default;
            MaxMp = default;
            Gold = default;
            if (Movement != null)
            {
                Movement.Dispose();
                Movement = null;
            }
            MessageObjectPool<PlayerInfo>.Return(this);
        }
        [ProtoMember(1)]
        public long RoleId { get; set; }
        [ProtoMember(2)]
        public string NickName { get; set; }
        [ProtoMember(3)]
        public long Hp { get; set; }
        [ProtoMember(4)]
        public long MaxHp { get; set; }
        [ProtoMember(5)]
        public long Mp { get; set; }
        [ProtoMember(6)]
        public long MaxMp { get; set; }
        [ProtoMember(7)]
        public long Gold { get; set; }
        [ProtoMember(8)]
        public MovementSnapshot Movement { get; set; }
    }
    /// <summary>
    /// 其他玩家可见的初始数据；Version 为服务端移动版本
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class PlayerPresence : AMessage, IDisposable
    {
        public static PlayerPresence Create(bool autoReturn = true)
        {
            var playerPresence = MessageObjectPool<PlayerPresence>.Rent();
            playerPresence.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                playerPresence.SetIsPool(false);
            }

            return playerPresence;
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
            RoleId = default;
            NickName = default;
            if (Movement != null)
            {
                Movement.Dispose();
                Movement = null;
            }
            Version = default;
            MessageObjectPool<PlayerPresence>.Return(this);
        }
        [ProtoMember(1)]
        public long RoleId { get; set; }
        [ProtoMember(2)]
        public string NickName { get; set; }
        [ProtoMember(3)]
        public MovementSnapshot Movement { get; set; }
        [ProtoMember(4)]
        public uint Version { get; set; }
    }
    /// <summary>
    /// 注册账号
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2A_RegisterRequest : AMessage, IRequest
    {
        public static C2A_RegisterRequest Create(bool autoReturn = true)
        {
            var c2A_RegisterRequest = MessageObjectPool<C2A_RegisterRequest>.Rent();
            c2A_RegisterRequest.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                c2A_RegisterRequest.SetIsPool(false);
            }

            return c2A_RegisterRequest;
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
            Username = default;
            Password = default;
            MessageObjectPool<C2A_RegisterRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2A_RegisterRequest; }
        [ProtoIgnore]
        public A2C_RegisterResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string Username { get; set; }
        [ProtoMember(2)]
        public string Password { get; set; }
    }
    /// <summary>
    /// 注册结果
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class A2C_RegisterResponse : AMessage, IResponse
    {
        public static A2C_RegisterResponse Create(bool autoReturn = true)
        {
            var a2C_RegisterResponse = MessageObjectPool<A2C_RegisterResponse>.Rent();
            a2C_RegisterResponse.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                a2C_RegisterResponse.SetIsPool(false);
            }

            return a2C_RegisterResponse;
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
            MessageObjectPool<A2C_RegisterResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.A2C_RegisterResponse; }
        [ProtoMember(2)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public ResultCode Result { get; set; }
    }
    /// <summary>
    /// 鉴权登录
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2A_LoginRequest : AMessage, IRequest
    {
        public static C2A_LoginRequest Create(bool autoReturn = true)
        {
            var c2A_LoginRequest = MessageObjectPool<C2A_LoginRequest>.Rent();
            c2A_LoginRequest.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                c2A_LoginRequest.SetIsPool(false);
            }

            return c2A_LoginRequest;
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
            Username = default;
            Password = default;
            MessageObjectPool<C2A_LoginRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2A_LoginRequest; }
        [ProtoIgnore]
        public A2C_LoginResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string Username { get; set; }
        [ProtoMember(2)]
        public string Password { get; set; }
    }
    /// <summary>
    /// 凭证和 Gate 地址分别返回，客户端不用解析 Token
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class A2C_LoginResponse : AMessage, IResponse
    {
        public static A2C_LoginResponse Create(bool autoReturn = true)
        {
            var a2C_LoginResponse = MessageObjectPool<A2C_LoginResponse>.Rent();
            a2C_LoginResponse.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                a2C_LoginResponse.SetIsPool(false);
            }

            return a2C_LoginResponse;
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
            Token = default;
            Result = default;
            GateAddress = default;
            MessageObjectPool<A2C_LoginResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.A2C_LoginResponse; }
        [ProtoMember(4)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public string Token { get; set; }
        [ProtoMember(2)]
        public ResultCode Result { get; set; }
        [ProtoMember(3)]
        public string GateAddress { get; set; }
    }
    /// <summary>
    /// 账号身份和目标 Gate 均从签名 Token 获取
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_LoginRequest : AMessage, IRequest
    {
        public static C2G_LoginRequest Create(bool autoReturn = true)
        {
            var c2G_LoginRequest = MessageObjectPool<C2G_LoginRequest>.Rent();
            c2G_LoginRequest.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                c2G_LoginRequest.SetIsPool(false);
            }

            return c2G_LoginRequest;
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
            Token = default;
            MessageObjectPool<C2G_LoginRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_LoginRequest; }
        [ProtoIgnore]
        public G2C_LoginResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string Token { get; set; }
    }
    /// <summary>
    /// Gate 登录结果
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_LoginResponse : AMessage, IResponse
    {
        public static G2C_LoginResponse Create(bool autoReturn = true)
        {
            var g2C_LoginResponse = MessageObjectPool<G2C_LoginResponse>.Rent();
            g2C_LoginResponse.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                g2C_LoginResponse.SetIsPool(false);
            }

            return g2C_LoginResponse;
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
            MessageObjectPool<G2C_LoginResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_LoginResponse; }
        [ProtoMember(2)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public ResultCode Result { get; set; }
    }
    /// <summary>
    /// 账号在另一条连接登录
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_RepeatLogin : AMessage, IMessage
    {
        public static G2C_RepeatLogin Create(bool autoReturn = true)
        {
            var g2C_RepeatLogin = MessageObjectPool<G2C_RepeatLogin>.Rent();
            g2C_RepeatLogin.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                g2C_RepeatLogin.SetIsPool(false);
            }

            return g2C_RepeatLogin;
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
            MessageObjectPool<G2C_RepeatLogin>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_RepeatLogin; }
    }
    /// <summary>
    /// 请求进入当前公共世界
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_EnterGameRequest : AMessage, IRequest
    {
        public static C2G_EnterGameRequest Create(bool autoReturn = true)
        {
            var c2G_EnterGameRequest = MessageObjectPool<C2G_EnterGameRequest>.Rent();
            c2G_EnterGameRequest.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                c2G_EnterGameRequest.SetIsPool(false);
            }

            return c2G_EnterGameRequest;
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
            MessageObjectPool<C2G_EnterGameRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_EnterGameRequest; }
        [ProtoIgnore]
        public G2C_EnterGameResponse ResponseType { get; set; }
    }
    /// <summary>
    /// Success 时必须携带 Info；RoleNotFound 时进入创建角色界面
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_EnterGameResponse : AMessage, IResponse
    {
        public static G2C_EnterGameResponse Create(bool autoReturn = true)
        {
            var g2C_EnterGameResponse = MessageObjectPool<G2C_EnterGameResponse>.Rent();
            g2C_EnterGameResponse.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                g2C_EnterGameResponse.SetIsPool(false);
            }

            return g2C_EnterGameResponse;
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
            if (Info != null)
            {
                Info.Dispose();
                Info = null;
            }
            MessageObjectPool<G2C_EnterGameResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_EnterGameResponse; }
        [ProtoMember(3)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public ResultCode Result { get; set; }
        [ProtoMember(2)]
        public PlayerInfo Info { get; set; }
    }
    /// <summary>
    /// 创建当前账号的唯一角色
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_CreateRoleRequest : AMessage, IRequest
    {
        public static C2G_CreateRoleRequest Create(bool autoReturn = true)
        {
            var c2G_CreateRoleRequest = MessageObjectPool<C2G_CreateRoleRequest>.Rent();
            c2G_CreateRoleRequest.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                c2G_CreateRoleRequest.SetIsPool(false);
            }

            return c2G_CreateRoleRequest;
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
            NickName = default;
            MessageObjectPool<C2G_CreateRoleRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_CreateRoleRequest; }
        [ProtoIgnore]
        public G2C_CreateRoleResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string NickName { get; set; }
    }
    /// <summary>
    /// 只返回创建结果，角色初始数据统一由 EnterGame 获取
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_CreateRoleResponse : AMessage, IResponse
    {
        public static G2C_CreateRoleResponse Create(bool autoReturn = true)
        {
            var g2C_CreateRoleResponse = MessageObjectPool<G2C_CreateRoleResponse>.Rent();
            g2C_CreateRoleResponse.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                g2C_CreateRoleResponse.SetIsPool(false);
            }

            return g2C_CreateRoleResponse;
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
            MessageObjectPool<G2C_CreateRoleResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_CreateRoleResponse; }
        [ProtoMember(2)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public ResultCode Result { get; set; }
    }
    /// <summary>
    /// 客户端移动采样；Sequence 属于当前连接，单调递增并允许 uint 回绕
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_PlayerMove : AMessage, IMessage
    {
        public static C2G_PlayerMove Create(bool autoReturn = true)
        {
            var c2G_PlayerMove = MessageObjectPool<C2G_PlayerMove>.Rent();
            c2G_PlayerMove.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                c2G_PlayerMove.SetIsPool(false);
            }

            return c2G_PlayerMove;
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
            Sequence = default;
            if (Movement != null)
            {
                Movement.Dispose();
                Movement = null;
            }
            MessageObjectPool<C2G_PlayerMove>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_PlayerMove; }
        [ProtoMember(1)]
        public uint Sequence { get; set; }
        [ProtoMember(2)]
        public MovementSnapshot Movement { get; set; }
    }
    /// <summary>
    /// 只用于其他玩家的移动；Version 由 Game 递增生成
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_PlayerMove : AMessage, IMessage
    {
        public static G2C_PlayerMove Create(bool autoReturn = true)
        {
            var g2C_PlayerMove = MessageObjectPool<G2C_PlayerMove>.Rent();
            g2C_PlayerMove.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                g2C_PlayerMove.SetIsPool(false);
            }

            return g2C_PlayerMove;
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
            RoleId = default;
            Version = default;
            if (Movement != null)
            {
                Movement.Dispose();
                Movement = null;
            }
            MessageObjectPool<G2C_PlayerMove>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_PlayerMove; }
        [ProtoMember(1)]
        public long RoleId { get; set; }
        [ProtoMember(2)]
        public uint Version { get; set; }
        [ProtoMember(3)]
        public MovementSnapshot Movement { get; set; }
    }
    /// <summary>
    /// 只发给本人；AckSequence 是本次被拒绝的上行序号，不是下行版本
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_PlayerPositionCorrection : AMessage, IMessage
    {
        public static G2C_PlayerPositionCorrection Create(bool autoReturn = true)
        {
            var g2C_PlayerPositionCorrection = MessageObjectPool<G2C_PlayerPositionCorrection>.Rent();
            g2C_PlayerPositionCorrection.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                g2C_PlayerPositionCorrection.SetIsPool(false);
            }

            return g2C_PlayerPositionCorrection;
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
            AckSequence = default;
            if (Movement != null)
            {
                Movement.Dispose();
                Movement = null;
            }
            MessageObjectPool<G2C_PlayerPositionCorrection>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_PlayerPositionCorrection; }
        [ProtoMember(1)]
        public uint AckSequence { get; set; }
        [ProtoMember(2)]
        public MovementSnapshot Movement { get; set; }
    }
    /// <summary>
    /// 创建或更新远端角色外观
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_PlayerEntered : AMessage, IMessage
    {
        public static G2C_PlayerEntered Create(bool autoReturn = true)
        {
            var g2C_PlayerEntered = MessageObjectPool<G2C_PlayerEntered>.Rent();
            g2C_PlayerEntered.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                g2C_PlayerEntered.SetIsPool(false);
            }

            return g2C_PlayerEntered;
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
            if (Player != null)
            {
                Player.Dispose();
                Player = null;
            }
            MessageObjectPool<G2C_PlayerEntered>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_PlayerEntered; }
        [ProtoMember(1)]
        public PlayerPresence Player { get; set; }
    }
    /// <summary>
    /// 删除远端角色
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_PlayerLeft : AMessage, IMessage
    {
        public static G2C_PlayerLeft Create(bool autoReturn = true)
        {
            var g2C_PlayerLeft = MessageObjectPool<G2C_PlayerLeft>.Rent();
            g2C_PlayerLeft.AutoReturn = autoReturn;

            if (!autoReturn)
            {
                g2C_PlayerLeft.SetIsPool(false);
            }

            return g2C_PlayerLeft;
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
            RoleId = default;
            MessageObjectPool<G2C_PlayerLeft>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_PlayerLeft; }
        [ProtoMember(1)]
        public long RoleId { get; set; }
    }
}