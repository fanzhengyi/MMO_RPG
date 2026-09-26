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
    [Serializable]
    [ProtoContract]
    public partial class RoleData : AMessage, IDisposable
    {
        public static RoleData Create(bool autoReturn = true)
        {
            var roleData = MessageObjectPool<RoleData>.Rent();
            roleData.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                roleData.SetIsPool(false);
            }
            
            return roleData;
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
            MessageObjectPool<RoleData>.Return(this);
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
            AccountErrorCode = default;
            if (Data != null)
            {
                Data.Dispose();
                Data = null;
            }
            MessageObjectPool<Game2G_EnterGameResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_EnterGameResponse; } 
        [ProtoMember(3)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public int AccountErrorCode { get; set; }
        [ProtoMember(2)]
        public RoleData Data { get; set; }
    }
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
            GateSessionRuntimeId = default;
            GateSceneAddress = default;
            MessageObjectPool<G2Game_CreateRoleRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_CreateRoleRequest; } 
        [ProtoIgnore]
        public Game2G_CreateRoleResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string UserName { get; set; }
        [ProtoMember(2)]
        public string NickName { get; set; }
        [ProtoMember(3)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(4)]
        public long GateSceneAddress { get; set; }
    }
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
            AccountErrorCode = default;
            if (Data != null)
            {
                Data.Dispose();
                Data = null;
            }
            MessageObjectPool<Game2G_CreateRoleResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_CreateRoleResponse; } 
        [ProtoMember(3)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public int AccountErrorCode { get; set; }
        [ProtoMember(2)]
        public RoleData Data { get; set; }
    }
}