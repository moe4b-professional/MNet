using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    #region Call
    public enum RpcType : byte
    {
        Broadcast, Target, Return
    }

    [Serializable]
    public enum RpcBufferMode : byte
    {
        None, Last, All
    }

    [Preserve]
    [Serializable]
    public struct RpcRequest : INetworkSerializable
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

        RpcBufferMode bufferMode;
        public RpcBufferMode BufferMode => bufferMode;

        NetworkClientID target;
        public NetworkClientID Target => target;

        NetworkClientID? exception;
        public NetworkClientID? Exception => exception;

        public void Except(NetworkClientID client)
        {
            exception = client;
        }

        ushort callback;
        public ushort Callback => callback;

        public object[] Read(IList<ParameterInfo> parameters)
        {
            using (var reader = new NetworkReader(raw))
            {
                var results = new object[parameters.Count];

                for (int i = 0; i < parameters.Count; i++)
                {
                    var value = reader.Read(parameters[i].ParameterType);

                    results[i] = value;
                }

                return results;
            }
        }

        public void Select(INetworkSerializableResolver.Context context)
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

                case RpcType.Return:
                    context.Select(ref target);
                    context.Select(ref callback);
                    break;
            }
        }

        public override string ToString() => $"RPC Request: {method}";

        //Static Utility

        public static byte[] Serialize(params object[] arguments)
        {
            Byte[] raw;

            using (var writer = new NetworkWriter(1024))
            {
                for (int i = 0; i < arguments.Length; i++)
                    writer.Write(arguments[i]);

                raw = writer.ToArray();
            }

            return raw;
        }

        public static RpcRequest Write(NetworkEntityID entity, NetworkBehaviourID behaviour, RpxMethodID method, RpcBufferMode bufferMode, params object[] arguments)
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
        public static RpcRequest Write(NetworkEntityID entity, NetworkBehaviourID behaviour, RpxMethodID method, NetworkClientID target, params object[] arguments)
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
        public static RpcRequest Write(NetworkEntityID entity, NetworkBehaviourID behaviour, RpxMethodID method, NetworkClientID target, ushort callback, params object[] arguments)
        {
            var raw = Serialize(arguments);

            var request = new RpcRequest()
            {
                entity = entity,
                behaviour = behaviour,
                method = method,
                raw = raw,
                type = RpcType.Return,
                target = target,
                callback = callback,
            };

            return request;
        }
    }

    [Preserve]
    [Serializable]
    public struct RpcCommand : INetworkSerializable
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

        ushort callback;
        public ushort Callback => callback;

        public object[] Read(IList<ParameterInfo> parameters, int optional)
        {
            using (var reader = new NetworkReader(raw))
            {
                var results = new object[parameters.Count];

                for (int i = 0; i < parameters.Count - optional; i++)
                {
                    var value = reader.Read(parameters[i].ParameterType);

                    results[i] = value;
                }

                return results;
            }
        }

        public void Select(INetworkSerializableResolver.Context context)
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

                case RpcType.Return:
                    context.Select(ref callback);
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
    }
    #endregion

    #region Return
    public enum RprResult : byte
    {
        Success,
        Disconnected,
        MethodNotFound,
        InvalidAuthority,
        InvalidArguments,
        RuntimeException,
        InvalidClient,
        InvalidEntity,
        InvalidBehaviour,
    }

    [Preserve]
    [Serializable]
    public struct RprRequest : INetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        NetworkClientID target;
        public NetworkClientID Target => target;

        ushort id;
        public ushort ID => id;

        RprResult result;
        public RprResult Result => result;

        byte[] raw;
        public byte[] Raw => raw;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref entity);
            context.Select(ref target);

            context.Select(ref id);

            context.Select(ref result);

            if (result == RprResult.Success) context.Select(ref raw);
        }

        public override string ToString() => $"RPR Request: {id}";

        public static RprRequest Write(NetworkEntityID entity, NetworkClientID target, ushort id, RprResult result)
        {
            var payload = new RprRequest()
            {
                entity = entity,
                target = target,
                id = id,
                result = result,
            };

            return payload;
        }
        public static RprRequest Write(NetworkEntityID entity, NetworkClientID target, ushort id, object value)
        {
            var raw = NetworkSerializer.Serialize(value);

            var payload = new RprRequest()
            {
                entity = entity,
                target = target,
                id = id,
                result = RprResult.Success,
                raw = raw,
            };

            return payload;
        }
    }

    [Preserve]
    [Serializable]
    public struct RprCommand : INetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        ushort id;
        public ushort ID => id;

        RprResult result;
        public RprResult Result => result;

        byte[] raw;
        public byte[] Raw => raw;

        public object Read(Type type)
        {
            var result = NetworkSerializer.Deserialize(raw, type);

            return result;
        }

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref entity);

            context.Select(ref id);
            context.Select(ref result);

            if (result == RprResult.Success) context.Select(ref raw);
        }

        public static RprCommand Write(NetworkEntityID entity, RprRequest request) => Write(entity, request.ID, request.Result, request.Raw);
        public static RprCommand Write(NetworkEntityID entity, ushort id, RprResult result, byte[] raw)
        {
            var payload = new RprCommand()
            {
                entity = entity,
                id = id,
                result = result,
                raw = raw,
            };

            return payload;
        }
        public static RprCommand Write(NetworkEntityID entity, ushort id, RprResult result)
        {
            var command = new RprCommand()
            {
                entity = entity,
                id = id,
                result = result,
            };

            return command;
        }
    }
    #endregion

    [Preserve]
    [Serializable]
    public struct RpxMethodID : INetworkSerializable
    {
        string value;
        public string Value { get { return value; } }

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref value);
        }

        public RpxMethodID(string value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(RpxMethodID))
            {
                var target = (RpxMethodID)obj;

                return target.value == this.value;
            }

            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(RpxMethodID a, RpxMethodID b) => a.Equals(b);
        public static bool operator !=(RpxMethodID a, RpxMethodID b) => !a.Equals(b);
    }
}