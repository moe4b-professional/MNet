using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
    [Serializable]
    public class NetworkGenericDictionary<TKey> : INetworkSerializable
    {
        Dictionary<TKey, byte[]> payload;

        Dictionary<TKey, object> objects;

        public IReadOnlyCollection<TKey> Keys => payload.Keys;

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
            var type = typeof(T);

            var writer = NetworkWriter.Pool.Any;

            writer.Write(type);
            writer.Write(value);

            var raw = writer.ToArray();

            NetworkWriter.Pool.Return(writer);

            payload[key] = raw;
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
                var reader = new NetworkReader(binary);

                reader.Read(out Type type);

                value = reader.Read(type);

                objects[key] = value;

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

        public void Select(ref NetworkSerializationContext context)
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