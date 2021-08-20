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
    public struct RpcID : IManualNetworkSerializable
    {
        byte value;
        public byte Value { get { return value; } }

        public void Serialize(NetworkStream writer)
        {
            writer.Insert(value);
        }

        public void Deserialize(NetworkStream reader)
        {
            value = reader.Take();
        }

        public RpcID(byte value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is RpcID target) return Equals(target);

            return false;
        }
        public bool Equals(RpcID target) => Equals(value, target.value);

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(RpcID a, RpcID b) => a.Equals(b);
        public static bool operator !=(RpcID a, RpcID b) => !a.Equals(b);
    }

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
    public interface IRpcRequest
    {
        NetworkEntityID Entity { get; }

        NetworkBehaviourID Behaviour { get; }

        RpcID Method { get; }

        byte[] Raw { get; }
    }

    [Preserve]
    public interface IRpcCommand
    {
        NetworkClientID Sender { get; }

        NetworkEntityID Entity { get; }

        NetworkBehaviourID Behaviour { get; }

        RpcID Method { get; }

        byte[] Raw { get; }
    }

    #region Broadcast
    [Preserve]
    public struct BroadcastRpcRequest : IRpcRequest, IManualNetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        RemoteBufferMode bufferMode;
        public RemoteBufferMode BufferMode => bufferMode;

        NetworkGroupID group;
        public NetworkGroupID Group => group;

        NetworkClientID? exception;
        public NetworkClientID? Exception => exception;

        public void Serialize(NetworkStream writer)
        {
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);

            writer.Write(raw);

            writer.Write(bufferMode);
            group.Serialize(writer);
            writer.Write(exception);
        }

        public void Deserialize(NetworkStream reader)
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

        public static BroadcastRpcRequest Write(
            NetworkEntityID entity,
            NetworkBehaviourID behaviour,
            RpcID method,
            RemoteBufferMode bufferMode,
            NetworkGroupID group,
            NetworkClientID? exception,
            byte[] raw)
        {
            var request = new BroadcastRpcRequest()
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
    public struct BroadcastRpcCommand : IRpcCommand, IManualNetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        public void Serialize(NetworkStream writer)
        {
            sender.Serialize(writer);
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);

            writer.Write(raw);
        }

        public void Deserialize(NetworkStream reader)
        {
            sender.Deserialize(reader);
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            method.Deserialize(reader);

            reader.Read(out raw);
        }

        //Static Utility

        public static BroadcastRpcCommand Write(NetworkClientID sender, BroadcastRpcRequest request)
        {
            var command = new BroadcastRpcCommand()
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
    public struct TargetRpcRequest : IRpcRequest, IManualNetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        NetworkClientID target;
        public NetworkClientID Target => target;

        public void Serialize(NetworkStream writer)
        {
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);

            writer.Write(raw);

            target.Serialize(writer);
        }

        public void Deserialize(NetworkStream reader)
        {
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            method.Deserialize(reader);

            reader.Read(out raw);

            target.Deserialize(reader);
        }

        public override string ToString() => $"RPC Request: {method}";

        //Static Utility

        public static TargetRpcRequest Write(
            NetworkEntityID entity,
            NetworkBehaviourID behaviour,
            RpcID method, NetworkClientID target,
            byte[] raw)
        {
            var request = new TargetRpcRequest()
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
    public struct TargetRpcCommand : IRpcCommand, IManualNetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        public void Serialize(NetworkStream writer)
        {
            sender.Serialize(writer);

            entity.Serialize(writer);
            behaviour.Serialize(writer);

            method.Serialize(writer);

            writer.Write(raw);
        }

        public void Deserialize(NetworkStream reader)
        {
            sender.Deserialize(reader);

            entity.Deserialize(reader);
            behaviour.Deserialize(reader);

            method.Deserialize(reader);

            reader.Read(out raw);
        }

        //Static Utility

        public static TargetRpcCommand Write(NetworkClientID sender, TargetRpcRequest request)
        {
            var command = new TargetRpcCommand()
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
    public struct QueryRpcRequest : IRpcRequest, IManualNetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        NetworkClientID target;
        public NetworkClientID Target => target;

        RprChannelID channel;
        public RprChannelID Channel => channel;

        public void Serialize(NetworkStream writer)
        {
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);

            writer.Write(raw);

            target.Serialize(writer);
            channel.Serialize(writer);
        }

        public void Deserialize(NetworkStream reader)
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

        public static QueryRpcRequest Write(
            NetworkEntityID entity,
            NetworkBehaviourID behaviour,
            RpcID method,
            NetworkClientID target,
            RprChannelID channel,
            byte[] raw)
        {
            var request = new QueryRpcRequest()
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
    public struct QueryRpcCommand : IRpcCommand, IManualNetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        RprChannelID channel;
        public RprChannelID Channel => channel;

        public void Serialize(NetworkStream writer)
        {
            sender.Serialize(writer);
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);

            writer.Write(raw);

            channel.Serialize(writer);
        }

        public void Deserialize(NetworkStream reader)
        {
            sender.Deserialize(reader);
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            method.Deserialize(reader);

            reader.Read(out raw);

            channel.Deserialize(reader);
        }

        //Static Utility

        public static QueryRpcCommand Write(NetworkClientID sender, QueryRpcRequest request)
        {
            var command = new QueryRpcCommand()
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

    #region Buffer
    [Preserve]
    public struct BufferRpcRequest : IRpcRequest, IManualNetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        RemoteBufferMode bufferMode;
        public RemoteBufferMode BufferMode => bufferMode;

        public void Serialize(NetworkStream writer)
        {
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);

            writer.Write(raw);

            writer.Write(bufferMode);
        }

        public void Deserialize(NetworkStream reader)
        {
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            method.Deserialize(reader);

            reader.Read(out raw);

            reader.Read(out bufferMode);
        }

        public override string ToString() => $"RPC Request: {method}";

        //Static Utility

        public static BufferRpcRequest Write(
            NetworkEntityID entity,
            NetworkBehaviourID behaviour,
            RpcID method,
            RemoteBufferMode bufferMode,
            byte[] raw)
        {
            var request = new BufferRpcRequest()
            {
                entity = entity,
                behaviour = behaviour,
                method = method,
                raw = raw,
                bufferMode = bufferMode,
            };

            return request;
        }
    }

    [Preserve]
    public struct BufferRpcCommand : IRpcCommand, IManualNetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        public void Serialize(NetworkStream writer)
        {
            sender.Serialize(writer);
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);

            writer.Write(raw);
        }

        public void Deserialize(NetworkStream reader)
        {
            sender.Deserialize(reader);
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            method.Deserialize(reader);

            reader.Read(out raw);
        }

        //Static Utility

        public static BufferRpcCommand Write(NetworkClientID sender, BufferRpcRequest request)
        {
            var command = new BufferRpcCommand()
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
}