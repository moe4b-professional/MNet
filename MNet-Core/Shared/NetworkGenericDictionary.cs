using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    public class NetworkGenericDictionary<TKey> : INetworkSerializable
    {
        Dictionary<TKey, byte[]> payload;

        Dictionary<TKey, object> objects;

        public IReadOnlyCollection<TKey> Keys => payload.Keys;

        public const int DefaultValueBufferSize = 256;

        public object this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value) == false)
                    return new KeyNotFoundException($"No Key: '{key}' registerd in {GetType().Name}");

                return value;
            }
        }

        public void Set<T>(TKey key, T value)
        {
            var code = NetworkPayload.GetCode<T>();

            using (var writer = new NetworkWriter(DefaultValueBufferSize))
            {
                writer.Write(code);
                writer.Write(value);

                var raw = writer.ToArray();
                payload[key] = raw;
            }

            objects[key] = value;
        }

        public bool Remove(TKey key)
        {
            objects.Remove(key);

            return payload.Remove(key);
        }

        public bool ContainsKey(TKey key) => payload.ContainsKey(key);

        public bool TryGetValue(TKey key, out object value)
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
        public bool TryGetValue<T>(TKey key, out T value)
        {
            if (TryGetValue(key, out object instance))
            {
                if (instance is T)
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
        }

        public NetworkGenericDictionary()
        {
            payload = new Dictionary<TKey, byte[]>();

            objects = new Dictionary<TKey, object>();
        }
    }
}