using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
    public struct RpcMethodID : IManualNetworkSerializable
    {
        byte value;
        public byte Value { get { return value; } }

        public void Serialize(NetworkWriter writer)
        {
            writer.Insert(value);
        }

        public void Deserialize(NetworkReader reader)
        {
            value = reader.Next();
        }

        public RpcMethodID(byte value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is RpcMethodID target) return Equals(target);

            return false;
        }
        public bool Equals(RpcMethodID target) => Equals(value, target.value);

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(RpcMethodID a, RpcMethodID b) => a.Equals(b);
        public static bool operator !=(RpcMethodID a, RpcMethodID b) => !a.Equals(b);
    }

    [Preserve]
    public interface IRpcRequest
    {
        NetworkEntityID Entity { get; }

        NetworkBehaviourID Behaviour { get; }

        RpcMethodID Method { get; }

        byte[] Raw { get; }
    }

    [Preserve]
    public interface IRpcCommand
    {
        NetworkClientID Sender { get; }

        NetworkEntityID Entity { get; }

        NetworkBehaviourID Behaviour { get; }

        RpcMethodID Method { get; }

        byte[] Raw { get; }
    }

    #region Broadcast
    [Preserve]
    public enum RemoteBufferMode : byte
    {
        /// <summary>
        /// No Buffering
        /// </summary>
        None,

        /// <summary>
        /// Buffers only the Latest Call
        /// </summary>
        Last,

        /// <summary>
        /// Buffers All Calls
        /// </summary>
        All
    }

    [Preserve]
    public struct RpcBroadcastRequest : IRpcRequest, IManualNetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcMethodID method;
        public RpcMethodID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        RemoteBufferMode bufferMode;
        public RemoteBufferMode BufferMode => bufferMode;

        NetworkGroupID group;
        public NetworkGroupID Group => group;

        NetworkClientID? exception;
        public NetworkClientID? Exception => exception;

        public void Serialize(NetworkWriter writer)
        {
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);

            writer.Write(raw);

            writer.Write(bufferMode);
            group.Serialize(writer);
            writer.Write(exception);
        }

        public void Deserialize(NetworkReader reader)
        {
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            method.Deserialize(reader);

            reader.Read(out raw);

            reader.Read(out bufferMode);
            group.Deserialize(reader);
            reader.Read(out exception);
        }

        public override string ToString() => $"RPC Request: {method}";

        //Static Utility

        public static RpcBroadcastRequest Write(
            NetworkEntityID entity,
            NetworkBehaviourID behaviour,
            RpcMethodID method,
            RemoteBufferMode bufferMode,
            NetworkGroupID group,
            NetworkClientID? exception,
            byte[] raw)
        {
            var request = new RpcBroadcastRequest()
            {
                entity = entity,
                behaviour = behaviour,
                method = method,
                raw = raw,
                bufferMode = bufferMode,
                exception = exception,
                group = group,
            };

            return request;
        }
    }

    [Preserve]
    public struct RpcBroadcastCommand : IRpcCommand, IManualNetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcMethodID method;
        public RpcMethodID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        public void Serialize(NetworkWriter writer)
        {
            sender.Serialize(writer);
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);

            writer.Write(raw);
        }

        public void Deserialize(NetworkReader reader)
        {
            sender.Deserialize(reader);
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            method.Deserialize(reader);

            reader.Read(out raw);
        }

        //Static Utility

        public static RpcBroadcastCommand Write(NetworkClientID sender, RpcBroadcastRequest request)
        {
            var command = new RpcBroadcastCommand()
            {
                sender = sender,
                entity = request.Entity,
                behaviour = request.Behaviour,
                method = request.Method,
                raw = request.Raw,
            };

            return command;
        }
    }
    #endregion

    #region Target
    [Preserve]
    public struct RpcTargetRequest : IRpcRequest, IManualNetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcMethodID method;
        public RpcMethodID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        NetworkClientID target;
        public NetworkClientID Target => target;

        public void Serialize(NetworkWriter writer)
        {
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);

            writer.Write(raw);

            target.Serialize(writer);
        }

        public void Deserialize(NetworkReader reader)
        {
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            method.Deserialize(reader);

            reader.Read(out raw);

            target.Deserialize(reader);
        }

        public override string ToString() => $"RPC Request: {method}";

        //Static Utility

        public static RpcTargetRequest Write(
            NetworkEntityID entity,
            NetworkBehaviourID behaviour,
            RpcMethodID method, NetworkClientID target,
            byte[] raw)
        {
            var request = new RpcTargetRequest()
            {
                entity = entity,
                behaviour = behaviour,
                method = method,
                raw = raw,
                target = target,
            };

            return request;
        }
    }

    [Preserve]
    public struct RpcTargetCommand : IRpcCommand, IManualNetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcMethodID method;
        public RpcMethodID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        public void Serialize(NetworkWriter writer)
        {
            sender.Serialize(writer);
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);

            writer.Write(raw);
        }

        public void Deserialize(NetworkReader reader)
        {
            sender.Deserialize(reader);
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            method.Deserialize(reader);
        }

        //Static Utility

        public static RpcTargetCommand Write(NetworkClientID sender, RpcTargetRequest request)
        {
            var command = new RpcTargetCommand()
            {
                sender = sender,
                entity = request.Entity,
                behaviour = request.Behaviour,
                method = request.Method,
                raw = request.Raw,
            };

            return command;
        }
    }
    #endregion

    #region Query
    [Preserve]
    public struct RpcQueryRequest : IRpcRequest, IManualNetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcMethodID method;
        public RpcMethodID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        NetworkClientID target;
        public NetworkClientID Target => target;

        RprChannelID channel;
        public RprChannelID Channel => channel;

        public void Serialize(NetworkWriter writer)
        {
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);

            writer.Write(raw);

            target.Serialize(writer);
            channel.Serialize(writer);
        }

        public void Deserialize(NetworkReader reader)
        {
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            method.Deserialize(reader);

            reader.Read(out raw);

            target.Deserialize(reader);
            channel.Deserialize(reader);
        }

        public override string ToString() => $"RPC Request: {method}";

        //Static Utility

        public static RpcQueryRequest Write(
            NetworkEntityID entity,
            NetworkBehaviourID behaviour,
            RpcMethodID method,
            NetworkClientID target,
            RprChannelID channel,
            byte[] raw)
        {
            var request = new RpcQueryRequest()
            {
                entity = entity,
                behaviour = behaviour,
                method = method,
                raw = raw,
                target = target,
                channel = channel,
            };

            return request;
        }
    }

    [Preserve]
    public struct RpcQueryCommand : IRpcCommand, IManualNetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcMethodID method;
        public RpcMethodID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        RprChannelID channel;
        public RprChannelID Channel => channel;

        public void Serialize(NetworkWriter writer)
        {
            sender.Serialize(writer);
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);

            writer.Write(raw);

            channel.Serialize(writer);
        }

        public void Deserialize(NetworkReader reader)
        {
            sender.Deserialize(reader);
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            method.Deserialize(reader);

            reader.Read(out raw);

            channel.Deserialize(reader);
        }

        //Static Utility

        public static RpcQueryCommand Write(NetworkClientID sender, RpcQueryRequest request)
        {
            var command = new RpcQueryCommand()
            {
                sender = sender,
                entity = request.Entity,
                behaviour = request.Behaviour,
                method = request.Method,
                raw = request.Raw,
                channel = request.Channel,
            };

            return command;
        }
    }
    #endregion
}