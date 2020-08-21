using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    [Serializable]
    [NetworkMessagePayload(3)]
    public sealed class RpcRequest : INetSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour { get { return behaviour; } }

        string method;
        public string Method { get { return method; } }

        RpcBufferMode buffer;
        public RpcBufferMode Buffer => buffer;

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
            writer.Write(buffer);
            writer.Write(raw);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out entity);
            reader.Read(out behaviour);
            reader.Read(out method);
            reader.Read(out buffer);
            reader.Read(out raw);
        }

        public RpcRequest() { }

        public static RpcRequest Write(NetworkEntityID entityID, NetworkBehaviourID behaviour, string method, RpcBufferMode buffer, params object[] arguments)
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
                buffer = buffer,
                raw = raw
            };

            return request;
        }
    }

    [Serializable]
    [NetworkMessagePayload(9)]
    public sealed class RpcCommand : INetSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender { get { return sender; } }

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

        public static RpcCommand Write(NetworkClientID sender, RpcRequest request)
        {
            var command = Write(sender, request.Entity, request.Behaviour, request.Method, request.Raw);

            return command;
        }
        public static RpcCommand Write(NetworkClientID sender, NetworkEntityID entity, NetworkBehaviourID behaviour, string method, byte[] raw)
        {
            var command = new RpcCommand()
            {
                sender = sender,
                entity = entity,
                behaviour = behaviour,
                method = method,
                raw = raw,
            };

            return command;
        }
    }

    [Serializable]
    public enum RpcBufferMode
    {
        None, Last, All
    }
}