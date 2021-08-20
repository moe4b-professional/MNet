using System;

using System.Linq;
using System.Text;

using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
    [Serializable]
    public class NetworkGenericDictionary<TKey> : IManualNetworkSerializable
    {
        Dictionary<TKey, byte[]> payload;

        Dictionary<TKey, object> objects;

        public IReadOnlyCollection<TKey> Keys => payload.Keys;

        public void Set<T>(TKey key, T value)
        {
            using (var stream = NetworkStream.Pool.Any)
            {
                stream.Write(value);

                var raw = stream.ToArray();

                payload[key] = raw;
                objects[key] = value;
            };
        }
        public T Get<T>(TKey key, T fallback = default)
        {
            if (TryGetValue<T>(key, out var value) == false)
                value = fallback;

            return value;
        }

        public bool ContainsKey(TKey key) => payload.ContainsKey(key);

        public bool TryGetValue<TValue>(TKey key, out TValue value)
        {
            if (objects.TryGetValue(key, out var instance))
            {
                if (instance is TValue cast)
                {
                    value = cast;
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            }

            if (payload.TryGetValue(key, out byte[] raw))
            {
                var reader = new NetworkStream(raw);

                try
                {
                    value = reader.Read<TValue>();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Exception when Reading key {key} as {typeof(TValue)}, Wrong Type Read Most Likely\n" +
                        $"Exception: {ex}");
                }

                objects[key] = value;

                return true;
            }

            value = default;
            return false;
        }

        public bool Remove(TKey key)
        {
            objects.Remove(key);

            return payload.Remove(key);
        }

        public int RemoveAll(IList<TKey> keys)
        {
            var count = 0;

            for (int i = 0; i < keys.Count; i++)
                if (Remove(keys[i]))
                    count += 1;

            return count;
        }

        public void CopyFrom(NetworkGenericDictionary<TKey> collection)
        {
            foreach (var key in collection.Keys)
            {
                objects.Remove(key);
                payload[key] = collection.payload[key];
            }
        }

        public void Serialize(NetworkStream writer)
        {
            NetworkSerializationHelper.Length.Write(writer, payload.Count);

            foreach (var pair in payload)
            {
                writer.Write(pair.Key);

                NetworkSerializationHelper.Length.Write(writer, pair.Value.Length);
                writer.Insert(pair.Value);
            }
        }
        public void Deserialize(NetworkStream reader)
        {
            NetworkSerializationHelper.Length.Read(reader, out var count);

            payload = new Dictionary<TKey, byte[]>(count);

            for (int i = 0; i < count; i++)
            {
                var key = reader.Read<TKey>();

                var length = NetworkSerializationHelper.Length.Read(reader);
                var raw = reader.Pull(length);

                payload.Add(key, raw);
            }
        }

        public NetworkGenericDictionary()
        {
            payload = new Dictionary<TKey, byte[]>();
            objects = new Dictionary<TKey, object>();
        }
    }
}