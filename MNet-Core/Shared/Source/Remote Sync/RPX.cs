using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    #region RPC
    public enum RpcType : byte
    {
        Broadcast,
        Target,
        Query,
        Response
    }

    [Preserve]
    [Serializable]
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
    [Serializable]
    public struct RpcRequest : IManualNetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcMethodID method;
        public RpcMethodID Method { get { return method; } }

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

        RpcMethodID callback;
        public RpcMethodID Callback => callback;

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

                case RpcType.Response:
                    context.Select(ref target);
                    break;

                case RpcType.Query:
                    context.Select(ref target);
                    context.Select(ref callback);
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

                case RpcType.Response:
                    target.Serialize(writer);
                    break;

                case RpcType.Query:
                    target.Serialize(writer);
                    callback.Serialize(writer);
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

                case RpcType.Response:
                    target.Deserialize(reader);
                    break;

                case RpcType.Query:
                    target.Deserialize(reader);
                    callback.Deserialize(reader);
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

        public static RpcRequest WriteBroadcast(NetworkEntityID entity, NetworkBehaviourID behaviour, RpcMethodID method, RemoteBufferMode bufferMode, params object[] arguments)
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

        public static RpcRequest WriteTarget(NetworkEntityID entity, NetworkBehaviourID behaviour, RpcMethodID method, NetworkClientID target, params object[] arguments)
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

        public static RpcRequest WriteQuery(NetworkEntityID entity, NetworkBehaviourID behaviour, RpcMethodID method, NetworkClientID target, RpcMethodID callback, params object[] arguments)
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
                callback = callback,
            };

            return request;
        }

        public static RpcRequest WriteResponse(NetworkEntityID entity, NetworkBehaviourID behaviour, RpcMethodID method, NetworkClientID target, params object[] arguments)
        {
            var raw = Serialize(arguments);

            var request = new RpcRequest()
            {
                entity = entity,
                behaviour = behaviour,
                method = method,
                raw = raw,
                type = RpcType.Response,
                target = target,
            };

            return request;
        }
    }

    [Preserve]
    [Serializable]
    public struct RpcCommand : IManualNetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        RpcMethodID method;
        public RpcMethodID Method { get { return method; } }

        NetworkTimeSpan time;
        public NetworkTimeSpan Time => time;

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        RpcType type;
        public RpcType Type => type;

        RpcMethodID callback;
        public RpcMethodID Callback => callback;

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

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref sender);
            context.Select(ref entity);
            context.Select(ref behaviour);
            context.Select(ref method);
            context.Select(ref time);

            context.Select(ref raw);

            context.Select(ref type);

            switch (type)
            {
                case RpcType.Broadcast:
                    break;

                case RpcType.Target:
                    break;

                case RpcType.Query:
                    context.Select(ref callback);
                    break;

                case RpcType.Response:
                    break;

                default:
                    Log.Error($"No Case Defined for {type} in {GetType()}");
                    break;
            }
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
                    callback.Serialize(writer);
                    break;

                case RpcType.Response:
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
                    callback.Deserialize(reader);
                    break;

                case RpcType.Response:
                    break;

                default:
                    Log.Error($"No Case Defined for {type} in {GetType()}");
                    break;
            }
        }

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
                callback = request.Callback,
                time = time,
            };

            return command;
        }

        public static RpcCommand Write(NetworkClientID sender, NetworkEntityID entity, NetworkBehaviourID behaviour, RpcMethodID method, RemoteResponseType result, NetworkTimeSpan time)
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

    [Serializable]
    public enum RemoteBufferMode : byte
    {
        None, Last, All
    }

    public enum RemoteResponseType : byte
    {
        Success,
        Disconnected,
        InvalidClient,
        InvalidEntity,
    }
}