using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RpcID : IEquatable<RpcID>, IComparable<RpcID>
    {
        byte value;
        public byte Value { get { return value; } }

        public RpcID(byte value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is RpcID target) return Equals(target);

            return false;
        }
        public bool Equals(RpcID target) => this.value == target.value;

        public int CompareTo(RpcID target) => this.value.CompareTo(target.value);

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

        ByteChunk Raw { get; }
    }

    [Preserve]
    public interface IRpcCommand
    {
        NetworkClientID Sender { get; }

        NetworkEntityID Entity { get; }

        NetworkBehaviourID Behaviour { get; }

        RpcID Method { get; }

        ByteChunk Raw { get; }
    }

    #region Broadcast
    [Preserve]
    public struct BroadcastRpcRequest : IRpcRequest, INetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        ByteChunk raw;
        public ByteChunk Raw { get { return raw; } }

        RemoteBufferMode bufferMode;
        public RemoteBufferMode BufferMode => bufferMode;

        NetworkGroupID group;
        public NetworkGroupID Group => group;

        NetworkClientID? exception;
        public NetworkClientID? Exception => exception;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref entity);
            context.Select(ref behaviour);
            context.Select(ref method);
            context.Select(ref raw);
            context.Select(ref bufferMode);
            context.Select(ref group);
            context.Select(ref exception);
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
            ByteChunk raw)
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
    public struct BroadcastRpcCommand : IRpcCommand, INetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        ByteChunk raw;
        public ByteChunk Raw { get { return raw; } }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref sender);
            context.Select(ref entity);
            context.Select(ref behaviour);
            context.Select(ref method);
            context.Select(ref raw);
        }

        //Static Utility

        public static BroadcastRpcCommand Write(NetworkClientID sender, BroadcastRpcRequest request)
        {
            var raw = request.BufferMode == RemoteBufferMode.None ? request.Raw : request.Raw.Clone();

            var command = new BroadcastRpcCommand()
            {
                sender = sender,
                entity = request.Entity,
                behaviour = request.Behaviour,
                method = request.Method,
                raw = raw,
            };

            return command;
        }
    }
    #endregion

    #region Target
    [Preserve]
    public struct TargetRpcRequest : IRpcRequest, INetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        ByteChunk raw;
        public ByteChunk Raw { get { return raw; } }

        NetworkClientID target;
        public NetworkClientID Target => target;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref entity);
            context.Select(ref behaviour);
            context.Select(ref method);
            context.Select(ref raw);
            context.Select(ref target);
        }

        public override string ToString() => $"RPC Request: {method}";

        //Static Utility

        public static TargetRpcRequest Write(
            NetworkEntityID entity,
            NetworkBehaviourID behaviour,
            RpcID method, NetworkClientID target,
            ByteChunk raw)
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
    public struct TargetRpcCommand : IRpcCommand, INetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        ByteChunk raw;
        public ByteChunk Raw { get { return raw; } }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref sender);
            context.Select(ref entity);
            context.Select(ref behaviour);
            context.Select(ref method);
            context.Select(ref raw);
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
    public struct QueryRpcRequest : IRpcRequest, INetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        ByteChunk raw;
        public ByteChunk Raw { get { return raw; } }

        NetworkClientID target;
        public NetworkClientID Target => target;

        RprChannelID channel;
        public RprChannelID Channel => channel;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref entity);
            context.Select(ref behaviour);
            context.Select(ref method);
            context.Select(ref raw);
            context.Select(ref target);
            context.Select(ref channel);
        }

        public override string ToString() => $"RPC Request: {method}";

        //Static Utility

        public static QueryRpcRequest Write(
            NetworkEntityID entity,
            NetworkBehaviourID behaviour,
            RpcID method,
            NetworkClientID target,
            RprChannelID channel,
            ByteChunk raw)
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
    public struct QueryRpcCommand : IRpcCommand, INetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        ByteChunk raw;
        public ByteChunk Raw { get { return raw; } }

        RprChannelID channel;
        public RprChannelID Channel => channel;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref sender);
            context.Select(ref entity);
            context.Select(ref behaviour);
            context.Select(ref method);
            context.Select(ref raw);
            context.Select(ref channel);
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
    public struct BufferRpcRequest : IRpcRequest, INetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        ByteChunk raw;
        public ByteChunk Raw { get { return raw; } }

        RemoteBufferMode bufferMode;
        public RemoteBufferMode BufferMode => bufferMode;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref entity);
            context.Select(ref behaviour);
            context.Select(ref method);
            context.Select(ref raw);
            context.Select(ref bufferMode);
        }

        public override string ToString() => $"RPC Request: {method}";

        //Static Utility

        public static BufferRpcRequest Write(
            NetworkEntityID entity,
            NetworkBehaviourID behaviour,
            RpcID method,
            RemoteBufferMode bufferMode,
            ByteChunk raw)
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
    public struct BufferRpcCommand : IRpcCommand, INetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcID method;
        public RpcID Method { get { return method; } }

        ByteChunk raw;
        public ByteChunk Raw { get { return raw; } }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref sender);
            context.Select(ref entity);
            context.Select(ref behaviour);
            context.Select(ref method);
            context.Select(ref raw);
        }

        //Static Utility

        public static BufferRpcCommand Write(NetworkClientID sender, BufferRpcRequest request)
        {
            var raw = request.Raw.Clone();

            var command = new BufferRpcCommand()
            {
                sender = sender,
                entity = request.Entity,
                behaviour = request.Behaviour,
                method = request.Method,
                raw = raw,
            };

            return command;
        }
    }
    #endregion
}