﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MNet
{
    [Preserve]
    [Serializable]
    public struct SyncVarFieldID : IManualNetworkSerializable
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

    public interface ISyncVarRequest
    {
        NetworkEntityID Entity { get; }

        NetworkBehaviourID Behaviour { get; }

        SyncVarFieldID Field { get; }

        NetworkGroupID Group { get; }

        byte[] Raw { get; }
    }

    public interface ISyncVarCommand
    {
        NetworkClientID Sender { get; }

        NetworkEntityID Entity { get; }

        NetworkBehaviourID Behaviour { get; }

        SyncVarFieldID Field { get; }

        byte[] Raw { get; }

        object Read(Type type);
    }

    #region Broadcast
    [Preserve]
    [Serializable]
    public struct BroadcastSyncVarRequest : ISyncVarRequest, IManualNetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour => behaviour;

        SyncVarFieldID field;
        public SyncVarFieldID Field => field;

        NetworkGroupID group;
        public NetworkGroupID Group => group;

        byte[] raw;
        public byte[] Raw => raw;

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

        public static BroadcastSyncVarRequest Write(NetworkEntityID entity, NetworkBehaviourID behaviour, SyncVarFieldID field, NetworkGroupID group, object value)
        {
            var raw = NetworkSerializer.Serialize(value);

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

        SyncVarFieldID field;
        public SyncVarFieldID Field => field;

        NetworkGroupID group;
        public NetworkGroupID Group => group;

        byte[] raw;
        public byte[] Raw => raw;

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

        public static BufferSyncVarRequest Write(NetworkEntityID entity, NetworkBehaviourID behaviour, SyncVarFieldID field, NetworkGroupID group, object value)
        {
            var raw = NetworkSerializer.Serialize(value);

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

        SyncVarFieldID field;
        public SyncVarFieldID Field => field;

        byte[] raw;
        public byte[] Raw => raw;

        public object Read(Type type)
        {
            var value = NetworkSerializer.Deserialize(raw, type);

            return value;
        }

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
    #endregion
}