using System;
using System.Collections.Generic;
using System.Text;

namespace MNet
{
    [Preserve]
    [Serializable]
    public struct SyncVarID : IManualNetworkSerializable
    {
        byte value;
        public byte Value { get { return value; } }

        public void Serialize(NetworkWriter writer)
        {
            writer.Insert(value);
        }

        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out value);
        }

        public SyncVarID(byte value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is SyncVarID target) return Equals(target);

            return false;
        }
        public bool Equals(SyncVarID target) => Equals(value, target.value);

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(SyncVarID a, SyncVarID b) => a.Equals(b);
        public static bool operator !=(SyncVarID a, SyncVarID b) => !a.Equals(b);
    }

    public interface ISyncVarRequest
    {
        NetworkEntityID Entity { get; }

        NetworkBehaviourID Behaviour { get; }

        SyncVarID Field { get; }

        NetworkGroupID Group { get; set; }

        ByteChunk Raw { get; }
    }
    public interface ISyncVarCommand
    {
        NetworkClientID Sender { get; }

        NetworkEntityID Entity { get; }

        NetworkBehaviourID Behaviour { get; }

        SyncVarID Field { get; }

        ByteChunk Raw { get; }
    }

    [Preserve]
    [Serializable]
    public struct BroadcastSyncVarRequest : ISyncVarRequest, IManualNetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour => behaviour;

        SyncVarID field;
        public SyncVarID Field => field;

        NetworkGroupID group;
        public NetworkGroupID Group
        {
            get => group;
            set => group = value;
        }

        ByteChunk raw;
        public ByteChunk Raw => raw;

        public void Serialize(NetworkWriter writer)
        {
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            field.Serialize(writer);
            group.Serialize(writer);

            writer.Write(raw);
        }
        public void Deserialize(NetworkReader reader)
        {
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            field.Deserialize(reader);
            group.Deserialize(reader);

            reader.Read(out raw);
        }

        public static BroadcastSyncVarRequest Write(NetworkEntityID entity, NetworkBehaviourID behaviour, SyncVarID field, NetworkGroupID group, ByteChunk raw)
        {
            var request = new BroadcastSyncVarRequest()
            {
                entity = entity,
                behaviour = behaviour,
                field = field,
                group = group,
                raw = raw,
            };

            return request;
        }
    }

    [Preserve]
    [Serializable]
    public struct BufferSyncVarRequest : ISyncVarRequest, IManualNetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour => behaviour;

        SyncVarID field;
        public SyncVarID Field => field;

        NetworkGroupID group;
        public NetworkGroupID Group
        {
            get => group;
            set => group = value;
        }

        ByteChunk raw;
        public ByteChunk Raw => raw;

        public void Serialize(NetworkWriter writer)
        {
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            field.Serialize(writer);
            group.Serialize(writer);

            writer.Write(raw);
        }
        public void Deserialize(NetworkReader reader)
        {
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            field.Deserialize(reader);
            group.Deserialize(reader);

            reader.Read(out raw);
        }

        public static BufferSyncVarRequest Write(NetworkEntityID entity, NetworkBehaviourID behaviour, SyncVarID field, NetworkGroupID group, ByteChunk raw)
        {
            var request = new BufferSyncVarRequest()
            {
                entity = entity,
                behaviour = behaviour,
                field = field,
                group = group,
                raw = raw,
            };

            return request;
        }
    }

    [Preserve]
    [Serializable]
    public struct SyncVarCommand : ISyncVarCommand, IManualNetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour => behaviour;

        SyncVarID field;
        public SyncVarID Field => field;

        ByteChunk raw;
        public ByteChunk Raw => raw;

        public void Serialize(NetworkWriter writer)
        {
            sender.Serialize(writer);
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            field.Serialize(writer);

            writer.Write(raw);
        }
        public void Deserialize(NetworkReader reader)
        {
            sender.Deserialize(reader);
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            field.Deserialize(reader);

            reader.Read(out raw);
        }

        public static SyncVarCommand Write<T>(NetworkClientID sender, T request)
            where T : ISyncVarRequest
        {
            var raw = request.Raw.Clone();

            var command = new SyncVarCommand()
            {
                sender = sender,
                entity = request.Entity,
                behaviour = request.Behaviour,
                field = request.Field,
                raw = raw,
            };

            return command;
        }
    }
}