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
    public class RpcRequest : INetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        string method;
        public string Method { get { return method; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        protected RpcType type;
        public RpcType Type => type;

        RpcBufferMode bufferMode;
        public RpcBufferMode BufferMode => bufferMode;

        protected NetworkClientID target;
        public NetworkClientID Target => target;

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

        public virtual void Select(INetworkSerializableResolver.Context context)
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

        public RpcRequest() { }

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

        public static RpcRequest Write(NetworkEntityID entity, NetworkBehaviourID behaviour, string method, RpcBufferMode bufferMode, params object[] arguments)
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
        public static RpcRequest Write(NetworkEntityID entity, NetworkBehaviourID behaviour, string method, NetworkClientID target, params object[] arguments)
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
        public static RpcRequest Write(NetworkEntityID entity, NetworkBehaviourID behaviour, string method, NetworkClientID target, ushort callback, params object[] arguments)
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
    public sealed class RpcCommand : INetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        string method;
        public string Method { get { return method; } }

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

        public RpcCommand() { }

        public static RpcCommand Write(NetworkClientID sender, RpcRequest request)
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
    public class RprRequest : INetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        NetworkClientID target;
        public NetworkClientID Target => target;

        ushort callback;
        public ushort Callback => callback;

        RprResult result;
        public RprResult Result => result;

        byte[] raw;
        public byte[] Raw => raw;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref entity);
            context.Select(ref target);

            context.Select(ref callback);

            context.Select(ref result);

            if (result == RprResult.Success) context.Select(ref raw);
        }

        public RprRequest()
        {

        }

        public static RprRequest Write(NetworkEntityID entity, NetworkClientID target, ushort callback, RprResult result)
        {
            var payload = new RprRequest()
            {
                entity = entity,
                target = target,
                callback = callback,
                result = result,
            };

            return payload;
        }
        public static RprRequest Write(NetworkEntityID entity, NetworkClientID target, ushort callback, object value)
        {
            var raw = NetworkSerializer.Serialize(value);

            var payload = new RprRequest()
            {
                entity = entity,
                target = target,
                callback = callback,
                result = RprResult.Success,
                raw = raw,
            };

            return payload;
        }
    }

    [Preserve]
    [Serializable]
    public class RprCommand : INetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        ushort callback;
        public ushort Callback => callback;

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

            context.Select(ref callback);
            context.Select(ref result);

            if(result == RprResult.Success) context.Select(ref raw);
        }

        public RprCommand()
        {

        }

        public static RprCommand Write(NetworkEntityID entity, RprRequest request) => Write(entity, request.Callback, request.Result, request.Raw);
        public static RprCommand Write(NetworkEntityID entity, ushort callback, RprResult result, byte[] raw)
        {
            var payload = new RprCommand()
            {
                entity = entity,
                callback = callback,
                result = result,
                raw = raw,
            };

            return payload;
        }
        public static RprCommand Write(NetworkEntityID entity, ushort callback, RprResult result)
        {
            var command = new RprCommand()
            {
                entity = entity,
                callback = callback,
                result = result,
            };

            return command;
        }
    }
    #endregion
}