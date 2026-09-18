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
    public partial class A2A_TestMessage : AMessage, IMessage
    {
        public static A2A_TestMessage Create(bool autoReturn = true)
        {
            var a2A_TestMessage = MessageObjectPool<A2A_TestMessage>.Rent();
            a2A_TestMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                a2A_TestMessage.SetIsPool(false);
            }
            
            return a2A_TestMessage;
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
            Tag = default;
            MessageObjectPool<A2A_TestMessage>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.A2A_TestMessage; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
}