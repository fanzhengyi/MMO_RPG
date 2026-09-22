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
            MessageObjectPool<Game2G_EnterGameResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_EnterGameResponse; } 
        [ProtoMember(13)]
        public uint ErrorCode { get; set; }
        [ProtoMember(1)]
        public int AccountErrorCode { get; set; }
        [ProtoMember(2)]
        public long RoleId { get; set; }
        [ProtoMember(3)]
        public string UserName { get; set; }
        [ProtoMember(4)]
        public long Hp { get; set; }
        [ProtoMember(5)]
        public long MaxHp { get; set; }
        [ProtoMember(6)]
        public long Mp { get; set; }
        [ProtoMember(7)]
        public long MaxMp { get; set; }
        [ProtoMember(8)]
        public long Gold { get; set; }
        [ProtoMember(9)]
        public float X { get; set; }
        [ProtoMember(10)]
        public float Y { get; set; }
        [ProtoMember(11)]
        public float Z { get; set; }
        [ProtoMember(12)]
        public float RotationY { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class Game2G_PlayerInfoUpdate : AMessage, IAddressMessage
    {
        public static Game2G_PlayerInfoUpdate Create(bool autoReturn = true)
        {
            var game2G_PlayerInfoUpdate = MessageObjectPool<Game2G_PlayerInfoUpdate>.Rent();
            game2G_PlayerInfoUpdate.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                game2G_PlayerInfoUpdate.SetIsPool(false);
            }
            
            return game2G_PlayerInfoUpdate;
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
            Hp = default;
            Mp = default;
            Gold = default;
            MessageObjectPool<Game2G_PlayerInfoUpdate>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_PlayerInfoUpdate; } 
        [ProtoMember(1)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(2)]
        public long RoleId { get; set; }
        [ProtoMember(3)]
        public long Hp { get; set; }
        [ProtoMember(4)]
        public long Mp { get; set; }
        [ProtoMember(5)]
        public long Gold { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2Game_PlayerDisconnect : AMessage, IAddressMessage
    {
        public static G2Game_PlayerDisconnect Create(bool autoReturn = true)
        {
            var g2Game_PlayerDisconnect = MessageObjectPool<G2Game_PlayerDisconnect>.Rent();
            g2Game_PlayerDisconnect.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Game_PlayerDisconnect.SetIsPool(false);
            }
            
            return g2Game_PlayerDisconnect;
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
            MessageObjectPool<G2Game_PlayerDisconnect>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Game_PlayerDisconnect; } 
        [ProtoMember(1)]
        public string UserName { get; set; }
        [ProtoMember(2)]
        public long GateSessionRuntimeId { get; set; }
    }
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
            X = default;
            Y = default;
            Z = default;
            RotationY = default;
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
        public float X { get; set; }
        [ProtoMember(5)]
        public float Y { get; set; }
        [ProtoMember(6)]
        public float Z { get; set; }
        [ProtoMember(7)]
        public float RotationY { get; set; }
    }
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
            X = default;
            Y = default;
            Z = default;
            RotationY = default;
            MessageObjectPool<Game2G_PlayerMove>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Game2G_PlayerMove; } 
        [ProtoMember(1)]
        public long GateSessionRuntimeId { get; set; }
        [ProtoMember(2)]
        public long RoleId { get; set; }
        [ProtoMember(3)]
        public float X { get; set; }
        [ProtoMember(4)]
        public float Y { get; set; }
        [ProtoMember(5)]
        public float Z { get; set; }
        [ProtoMember(6)]
        public float RotationY { get; set; }
    }
}