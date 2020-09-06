using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    public class AttributesCollection : INetworkSerializable
    {
        Dictionary<string, byte[]> payload;

        Dictionary<string, object> objects;

        public IReadOnlyCollection<string> Keys => payload.Keys;

        public const int DefaultValueBufferSize = 256;

        public object this[string key]
        {
            get
            {
                if (TryGetValue(key, out var value) == false)
                    return new KeyNotFoundException($"No Key: '{key}' registerd in {nameof(AttributesCollection)}");

                return value;
            }
        }

        public void Set<T>(string key, T value)
        {
            using (var writer = new NetworkWriter(DefaultValueBufferSize))
            {
                var code = NetworkPayload.GetCode<T>();

                writer.Write(code);
                writer.Write(value);

                var binary = writer.ToArray();
                payload[key] = binary;
            }

            objects[key] = value;
        }

        public bool Remove(string key)
        {
            objects.Remove(key);

            return payload.Remove(key);
        }

        public bool ContainsKey(string key) => payload.ContainsKey(key);

        public bool TryGetValue(string key, out object value)
        {
            if (objects.TryGetValue(key, out value)) return true;

            if (payload.TryGetValue(key, out byte[] binary))
            {
                using (var reader = new NetworkReader(binary))
                {
                    reader.Read(out ushort code);

                    var type = NetworkPayload.GetType(code);

                    value = reader.Read(type);

                    objects[key] = value;
                }

                return true;
            }

            return false;
        }
        public bool TryGetValue<T>(string key, out T value)
        {
            if (TryGetValue(key, out object instance))
            {
                if(instance is T)
                {
                    value = (T)instance;
                    return true;
                }
            }

            value = default(T);
            return false;
        }

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref payload);

            if (context.IsReading) objects = new Dictionary<string, object>();
        }

        public AttributesCollection()
        {
            payload = new Dictionary<string, byte[]>();

            objects = new Dictionary<string, object>();
        }
    }
}