using System;
using System.Collections.Generic;
using System.Text;

namespace MNet
{
    [Preserve]
    [Serializable]
    public struct SyncVarFieldID : INetworkSerializable
    {
        byte value;
        public byte Value { get { return value; } }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref value);
        }

        public SyncVarFieldID(byte value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is SyncVarFieldID target) return Equals(target);

            return false;
        }
        public bool Equals(SyncVarFieldID target) => Equals(value, target.value);

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(SyncVarFieldID a, SyncVarFieldID b) => a.Equals(b);
        public static bool operator !=(SyncVarFieldID a, SyncVarFieldID b) => !a.Equals(b);
    }

    [Preserve]
    [Serializable]
    public struct SyncVarRequest : INetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour => behaviour;

        SyncVarFieldID field;
        public SyncVarFieldID Field => field;

        byte[] raw;
        public byte[] Raw => raw;

        public object Read(Type type)
        {
            var value = NetworkSerializer.Deserialize(raw, type);

            return value;
        }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref entity);
            context.Select(ref behaviour);
            context.Select(ref field);
            context.Select(ref raw);
        }

        public static SyncVarRequest Write(NetworkEntityID entity, NetworkBehaviourID behaviour, SyncVarFieldID field, object value)
        {
            var raw = NetworkSerializer.Serialize(value);

            var request = new SyncVarRequest()
            {
                entity = entity,
                behaviour = behaviour,
                field = field,
                raw = raw,
            };

            return request;
        }
    }

    [Preserve]
    [Serializable]
    public struct SyncVarCommand : INetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour => behaviour;

        SyncVarFieldID field;
        public SyncVarFieldID Field => field;

        byte[] raw;
        public byte[] Raw => raw;

        public object Read(Type type)
        {
            var value = NetworkSerializer.Deserialize(raw, type);

            return value;
        }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref sender);
            context.Select(ref entity);
            context.Select(ref behaviour);
            context.Select(ref field);
            context.Select(ref raw);
        }

        public static SyncVarCommand Write(NetworkClientID sender, SyncVarRequest request)
        {
            return Write(sender, request.Entity, request.Behaviour, request.Field, request.Raw);
        }
        public static SyncVarCommand Write(NetworkClientID sender, NetworkEntityID entity, NetworkBehaviourID behaviour, SyncVarFieldID field, byte[] raw)
        {
            var request = new SyncVarCommand()
            {
                sender = sender,
                entity = entity,
                behaviour = behaviour,
                field = field,
                raw = raw,
            };

            return request;
        }
    }
}