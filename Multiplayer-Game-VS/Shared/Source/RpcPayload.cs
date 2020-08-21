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
    public sealed class RpcPayload : INetSerializable
    {
        string entity;
        public string Entity { get { return entity; } }

        string behaviour;
        public string Behaviour { get { return behaviour; } }

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

        public RpcPayload() { }
        public RpcPayload(string entity, string behaviour, string method, RpcBufferMode buffer, byte[] raw)
        {
            this.entity = entity;
            this.behaviour = behaviour;
            this.method = method;
            this.raw = raw;
        }

        public static RpcPayload Write(string entity, string behaviour, string method, RpcBufferMode buffer, params object[] arguments)
        {
            var writer = new NetworkWriter(1024);

            for (int i = 0; i < arguments.Length; i++)
                writer.Write(arguments[i]);

            var raw = writer.ToArray();

            var payload = new RpcPayload(entity, behaviour, method, buffer, raw);

            return payload;
        }
    }

    [Serializable]
    public enum RpcBufferMode
    {
        None, Last, All
    }
}