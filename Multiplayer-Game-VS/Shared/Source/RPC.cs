using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    [Serializable]
    public sealed class RpcRequest : INetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        string method;
        public string Method { get { return method; } }

        RpcBufferMode bufferMode;
        public RpcBufferMode BufferMode => bufferMode;

        private byte[] raw;
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

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(entity);
            writer.Write(behaviour);
            writer.Write(method);
            writer.Write(bufferMode);
            writer.Write(raw);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out entity);
            reader.Read(out behaviour);
            reader.Read(out method);
            reader.Read(out bufferMode);
            reader.Read(out raw);
        }

        public RpcRequest() { }

        public static RpcRequest Write(NetworkEntityID entityID, NetworkBehaviourID behaviour, string method, RpcBufferMode bufferMode, params object[] arguments)
        {
            Byte[] raw;

            using (var writer = new NetworkWriter(1024))
            {
                for (int i = 0; i < arguments.Length; i++)
                    writer.Write(arguments[i]);

                raw = writer.ToArray();
            }

            var request = new RpcRequest()
            {
                entity = entityID,
                behaviour = behaviour,
                method = method,
                bufferMode = bufferMode,
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

        private byte[] raw;
        public byte[] Raw { get { return raw; } }

        public object[] Read(IList<ParameterInfo> parameters)
        {
            using (var reader = new NetworkReader(raw))
            {
                var results = new object[parameters.Count];

                for (int i = 0; i < parameters.Count - 1; i++)
                {
                    var value = reader.Read(parameters[i].ParameterType);

                    results[i] = value;
                }

                return results;
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(sender);
            writer.Write(entity);
            writer.Write(behaviour);
            writer.Write(method);
            writer.Write(raw);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out sender);
            reader.Read(out entity);
            reader.Read(out behaviour);
            reader.Read(out method);
            reader.Read(out raw);
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