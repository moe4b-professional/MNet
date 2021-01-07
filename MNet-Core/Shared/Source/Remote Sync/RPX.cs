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
    [Serializable]
    public struct RpxMethodID : IManualNetworkSerializable
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

        public RpxMethodID(byte value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is RpxMethodID target) return Equals(target);

            return false;
        }
        public bool Equals(RpxMethodID target) => Equals(value, target.value);

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(RpxMethodID a, RpxMethodID b) => a.Equals(b);
        public static bool operator !=(RpxMethodID a, RpxMethodID b) => !a.Equals(b);
    }

    [Serializable]
    public enum RemoteBufferMode : byte
    {
        None, Last, All
    }

    public enum RemoteResponseType : byte
    {
        Success,
        Disconnect,
        InvalidClient,
        InvalidEntity,
        FatalFailure,
    }

    #region RPC
    public enum RpcType : byte
    {
        Broadcast,
        Target,
        Query,
    }

    [Preserve]
    public struct RpcRequest : IManualNetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpxMethodID method;
        public RpxMethodID Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        RpcType type;
        public RpcType Type => type;

        RemoteBufferMode bufferMode;
        public RemoteBufferMode BufferMode => bufferMode;

        NetworkClientID target;
        public NetworkClientID Target => target;

        NetworkClientID? exception;
        public NetworkClientID? Exception => exception;

        public void Except(NetworkClientID client)
        {
            exception = client;
        }

        RprChannelID returnChannel;
        public RprChannelID ReturnChannel => returnChannel;

        public object[] Read(IList<ParameterInfo> parameters)
        {
            var reader = new NetworkReader(raw);

            var results = new object[parameters.Count];

            for (int i = 0; i < parameters.Count; i++)
            {
                var value = reader.Read(parameters[i].ParameterType);

                results[i] = value;
            }

            return results;
        }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref entity);
            context.Select(ref behaviour);
            context.Select(ref method);

            context.Select(ref raw);

            context.Select(ref type);

            switch (type)
            {
                case RpcType.Broadcast:
                    context.Select(ref bufferMode);
                    context.Select(ref exception);
                    break;

                case RpcType.Target:
                    context.Select(ref target);
                    break;

                case RpcType.Query:
                    context.Select(ref target);
                    context.Select(ref returnChannel);
                    break;

                default:
                    Log.Error($"No Case Defined for {type} in {GetType()}");
                    break;
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);

            writer.Write(raw);

            writer.Write(type);

            switch (type)
            {
                case RpcType.Broadcast:
                    writer.Write(bufferMode);
                    writer.Write(exception);
                    break;

                case RpcType.Target:
                    target.Serialize(writer);
                    break;

                case RpcType.Query:
                    target.Serialize(writer);
                    returnChannel.Serialize(writer);
                    break;

                default:
                    Log.Error($"No Case Defined for {type} in {GetType()}");
                    break;
            }
        }

        public void Deserialize(NetworkReader reader)
        {
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            method.Deserialize(reader);

            reader.Read(out raw);

            reader.Read(out type);

            switch (type)
            {
                case RpcType.Broadcast:
                    reader.Read(out bufferMode);
                    reader.Read(out exception);
                    break;

                case RpcType.Target:
                    target.Deserialize(reader);
                    break;

                case RpcType.Query:
                    target.Deserialize(reader);
                    returnChannel.Deserialize(reader);
                    break;

                default:
                    Log.Error($"No Case Defined for {type} in {GetType()}");
                    break;
            }
        }

        public override string ToString() => $"RPC Request: {method}";

        //Static Utility

        public static byte[] Serialize(params object[] arguments)
        {
            var writer = NetworkWriter.Pool.Any;

            for (int i = 0; i < arguments.Length; i++)
                writer.Write(arguments[i]);

            var raw = writer.ToArray();

            NetworkWriter.Pool.Return(writer);

            return raw;
        }

        public static RpcRequest WriteBroadcast(NetworkEntityID entity, NetworkBehaviourID behaviour, RpxMethodID method, RemoteBufferMode bufferMode, params object[] arguments)
        {
            var raw = Serialize(arguments);

            var request = new RpcRequest()
            {
                entity = entity,
                behaviour = behaviour,
                method = method,
                raw = raw,
                type = RpcType.Broadcast,
                bufferMode = bufferMode,
            };

            return request;
        }

        public static RpcRequest WriteTarget(NetworkEntityID entity, NetworkBehaviourID behaviour, RpxMethodID method, NetworkClientID target, params object[] arguments)
        {
            var raw = Serialize(arguments);

            var request = new RpcRequest()
            {
                entity = entity,
                behaviour = behaviour,
                method = method,
                raw = raw,
                type = RpcType.Target,
                target = target,
            };

            return request;
        }

        public static RpcRequest WriteQuery(NetworkEntityID entity, NetworkBehaviourID behaviour, RpxMethodID method, NetworkClientID target, RprChannelID returnChannel, params object[] arguments)
        {
            var raw = Serialize(arguments);

            var request = new RpcRequest()
            {
                entity = entity,
                behaviour = behaviour,
                method = method,
                raw = raw,
                type = RpcType.Query,
                target = target,
                returnChannel = returnChannel,
            };

            return request;
        }
    }

    [Preserve]
    public struct RpcCommand : IManualNetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpxMethodID method;
        public RpxMethodID Method { get { return method; } }

        NetworkTimeSpan time;
        public NetworkTimeSpan Time => time;

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        RpcType type;
        public RpcType Type => type;

        RprChannelID returnChannel;
        public RprChannelID ReturnChannel => returnChannel;

        public object[] Read(IList<ParameterInfo> parameters, int optional)
        {
            var reader = new NetworkReader(raw);

            var results = new object[parameters.Count];

            for (int i = 0; i < parameters.Count - optional; i++)
            {
                var value = reader.Read(parameters[i].ParameterType);

                results[i] = value;

                if (i == 0 && value is RemoteResponseType rpr && rpr != RemoteResponseType.Success && parameters.Count > 1)
                {
                    results[i + 1] = null;
                    break;
                }
            }

            return results;
        }

        public void Serialize(NetworkWriter writer)
        {
            sender.Serialize(writer);
            entity.Serialize(writer);
            behaviour.Serialize(writer);
            method.Serialize(writer);
            time.Serialize(writer);

            writer.Write(raw);

            writer.Write(type);

            switch (type)
            {
                case RpcType.Broadcast:
                    break;

                case RpcType.Target:
                    break;

                case RpcType.Query:
                    returnChannel.Serialize(writer);
                    break;

                default:
                    Log.Error($"No Case Defined for {type} in {GetType()}");
                    break;
            }
        }

        public void Deserialize(NetworkReader reader)
        {
            sender.Deserialize(reader);
            entity.Deserialize(reader);
            behaviour.Deserialize(reader);
            method.Deserialize(reader);
            time.Deserialize(reader);

            reader.Read(out raw);

            reader.Read(out type);

            switch (type)
            {
                case RpcType.Broadcast:
                    break;

                case RpcType.Target:
                    break;

                case RpcType.Query:
                    returnChannel.Deserialize(reader);
                    break;

                default:
                    Log.Error($"No Case Defined for {type} in {GetType()}");
                    break;
            }
        }

        //Static Utility

        public static RpcCommand Write(NetworkClientID sender, RpcRequest request, NetworkTimeSpan time)
        {
            var command = new RpcCommand()
            {
                sender = sender,
                entity = request.Entity,
                behaviour = request.Behaviour,
                method = request.Method,
                raw = request.Raw,
                type = request.Type,
                returnChannel = request.ReturnChannel,
                time = time,
            };

            return command;
        }

        public static RpcCommand Write(NetworkClientID sender, NetworkEntityID entity, NetworkBehaviourID behaviour, RpxMethodID method, RemoteResponseType result, NetworkTimeSpan time)
        {
            var raw = NetworkSerializer.Serialize(result);

            var command = new RpcCommand()
            {
                sender = sender,
                entity = entity,
                behaviour = behaviour,
                method = method,
                raw = raw,
                type = RpcType.Target,
                time = time,
            };

            return command;
        }
    }
    #endregion

    #region RPR
    [Preserve]
    public struct RprChannelID : IManualNetworkSerializable
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

        public RprChannelID(byte value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is RprChannelID target) return Equals(target);

            return false;
        }
        public bool Equals(RprChannelID target) => Equals(value, target.value);

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(RprChannelID a, RprChannelID b) => a.Equals(b);
        public static bool operator !=(RprChannelID a, RprChannelID b) => !a.Equals(b);

        public static RprChannelID Increment(RprChannelID channel) => new RprChannelID((byte)(channel.value + 1));
    }

    [Preserve]
    public struct RprRequest : INetworkSerializable
    {
        NetworkClientID target;
        public NetworkClientID Target => target;

        RprChannelID channel;
        public RprChannelID Channel => channel;

        RemoteResponseType response;
        public RemoteResponseType Response => response;

        byte[] raw;
        public byte[] Raw => raw;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref target);
            context.Select(ref channel);
            context.Select(ref response);
            context.Select(ref raw);
        }

        public RprRequest(NetworkClientID target, RprChannelID channel, RemoteResponseType response, byte[] raw)
        {
            this.target = target;
            this.channel = channel;
            this.response = response;
            this.raw = raw;
        }

        public static RprRequest Write(NetworkClientID target, RprChannelID channel, object result, Type type)
        {
            var raw = NetworkSerializer.Serialize(result, type);

            var request = new RprRequest(target, channel, RemoteResponseType.Success, raw);
            return request;
        }

        public static RprRequest Write(NetworkClientID target, RprChannelID channel, RemoteResponseType response)
        {
            var request = new RprRequest(target, channel, response, new byte[0]);
            return request;
        }
    }

    [Preserve]
    public struct RprResponse : INetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        RprChannelID channel;
        public RprChannelID Channel => channel;

        RemoteResponseType response;
        public RemoteResponseType Response => response;

        byte[] raw;
        public byte[] Raw => raw;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref sender);
            context.Select(ref channel);
            context.Select(ref response);
            context.Select(ref raw);
        }

        public RprResponse(NetworkClientID sender, RprChannelID channel, RemoteResponseType response, byte[] raw)
        {
            this.sender = sender;
            this.channel = channel;
            this.response = response;
            this.raw = raw;
        }

        public static RprResponse Write(NetworkClientID sender, RprRequest request)
        {
            var command = new RprResponse(sender, request.Channel, request.Response, request.Raw);
            return command;
        }
    }

    [Preserve]
    public struct RprCommand : INetworkSerializable
    {
        RprChannelID channel;
        public RprChannelID Channel => channel;

        RemoteResponseType response;
        public RemoteResponseType Response => response;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref channel);
            context.Select(ref response);
        }

        public RprCommand(RprChannelID channel, RemoteResponseType response)
        {
            this.channel = channel;
            this.response = response;
        }

        public static RprCommand Write(RprChannelID channel, RemoteResponseType response)
        {
            var request = new RprCommand(channel, response);
            return request;
        }
    }
    #endregion
}