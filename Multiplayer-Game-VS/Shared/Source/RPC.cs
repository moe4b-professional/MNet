using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    [Serializable]
    public abstract class RpcRequest : INetworkSerializable
    {
        protected NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        protected NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        protected string method;
        public string Method { get { return method; } }

        protected byte[] raw;
        public byte[] Raw { get { return raw; } }

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
    }

    [Serializable]
    public class BroadcastRpcRequest : RpcRequest
    {
        RpcBufferMode bufferMode;
        public RpcBufferMode BufferMode => bufferMode;

        public override void Select(INetworkSerializableResolver.Context context)
        {
            base.Select(context);

            context.Select(ref bufferMode);
        }

        public static BroadcastRpcRequest Write(NetworkEntityID entity, NetworkBehaviourID behaviour, string method, RpcBufferMode bufferMode, params object[] arguments)
        {
            var raw = Serialize(arguments);

            var request = new BroadcastRpcRequest()
            {
                entity = entity,
                behaviour = behaviour,
                method = method,
                bufferMode = bufferMode,
                raw = raw
            };

            return request;
        }
    }

    [Serializable]
    public class TargetRpcRequest : RpcRequest
    {
        NetworkClientID client;
        public NetworkClientID Client => client;

        public override void Select(INetworkSerializableResolver.Context context)
        {
            base.Select(context);

            context.Select(ref client);
        }

        public static TargetRpcRequest Write(NetworkClientID client, NetworkEntityID entity, NetworkBehaviourID behaviour, string method, params object[] arguments)
        {
            var raw = Serialize(arguments);

            var request = new TargetRpcRequest()
            {
                client = client,
                entity = entity,
                behaviour = behaviour,
                method = method,
                raw = raw
            };

            return request;
        }
    }

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
        }

        public RpcCommand() { }
        public RpcCommand(NetworkClientID sender, NetworkEntityID entity, NetworkBehaviourID behaviour, string method, byte[] raw)
        {
            this.sender = sender;
            this.entity = entity;
            this.method = method;
            this.behaviour = behaviour;
            this.raw = raw;
        }
    }

    [Serializable]
    public enum RpcBufferMode
    {
        None, Last, All
    }
}