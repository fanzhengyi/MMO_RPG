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
    /// 注册请求
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
    /// 注册响应
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
            LoginError = default;
            MessageObjectPool<A2C_RegisterResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.A2C_RegisterResponse; } 
        [ProtoMember(2)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public AccountErrorCode LoginError { get; set; }
    }
    /// <summary>
    /// 登录请求（Authentication验证，成功返回Token）
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
    /// 登录响应
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
            LoginError = default;
            MessageObjectPool<A2C_LoginResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.A2C_LoginResponse; } 
        [ProtoMember(3)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public string Token { get; set; }
        [ProtoMember(2)]
        public AccountErrorCode LoginError { get; set; }
    }
    /// <summary>
    /// 客户端登录到Gate服务器（带Token验证）
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
            UserName = default;
            MessageObjectPool<C2G_LoginRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_LoginRequest; } 
        [ProtoIgnore]
        public G2C_LoginResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string Token { get; set; }
        [ProtoMember(2)]
        public string UserName { get; set; }
    }
    /// <summary>
    /// Gate登录响应
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
            LoginError = default;
            MessageObjectPool<G2C_LoginResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_LoginResponse; } 
        [ProtoMember(2)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public AccountErrorCode LoginError { get; set; }
    }
    /// <summary>
    /// 有人顶号的时候发送
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G_2C_RepeaLogin : AMessage, IMessage
    {
        public static G_2C_RepeaLogin Create(bool autoReturn = true)
        {
            var g_2C_RepeaLogin = MessageObjectPool<G_2C_RepeaLogin>.Rent();
            g_2C_RepeaLogin.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g_2C_RepeaLogin.SetIsPool(false);
            }
            
            return g_2C_RepeaLogin;
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
            MessageObjectPool<G_2C_RepeaLogin>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G_2C_RepeaLogin; } 
    }
    /// <summary>
    /// 玩家信息（进游戏/创角返回，客户端刷新UI用）
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
            UserName = default;
            Hp = default;
            MaxHp = default;
            Mp = default;
            MaxMp = default;
            Gold = default;
            X = default;
            Y = default;
            Z = default;
            RotationY = default;
            NickName = default;
            MessageObjectPool<PlayerInfo>.Return(this);
        }
        [ProtoMember(1)]
        public long RoleId { get; set; }
        [ProtoMember(2)]
        public string UserName { get; set; }
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
        public float X { get; set; }
        [ProtoMember(9)]
        public float Y { get; set; }
        [ProtoMember(10)]
        public float Z { get; set; }
        [ProtoMember(11)]
        public float RotationY { get; set; }
        [ProtoMember(12)]
        public string NickName { get; set; }
    }
    /// <summary>
    /// 登录成功后请求进入游戏（服务端判断是否有角色）
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
    /// 进入游戏响应：错误码 + 玩家信息
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
            AccountErrorCode = default;
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
        public int AccountErrorCode { get; set; }
        [ProtoMember(2)]
        public PlayerInfo Info { get; set; }
    }
    /// <summary>
    /// 客户端请求创建角色（当前只有默认职业，只传昵称）
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
    /// 创建角色响应
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
            AccountErrorCode = default;
            if (Info != null)
            {
                Info.Dispose();
                Info = null;
            }
            MessageObjectPool<G2C_CreateRoleResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_CreateRoleResponse; } 
        [ProtoMember(3)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public int AccountErrorCode { get; set; }
        [ProtoMember(2)]
        public PlayerInfo Info { get; set; }
    }
}