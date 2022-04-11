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
        public IReadOnlyCollection<TKey> Keys => payload.Keys;

        Dictionary<TKey, object> cache;

        public void Set<[NetworkSerializationGenerator] TValue>(TKey key, TValue value)
        {
            using (NetworkWriter.Pool.Lease(out var stream))
            {
                stream.Write(value);

                var raw = stream.ToArray();

                payload[key] = raw;
                cache[key] = value;
            };
        }

        public TValue Get<[NetworkSerializationGenerator] TValue>(TKey key)
        {
            if (TryGetValue<TValue>(key, out var value) == false)
                value = default;

            return value;
        }

        public bool TryGetValue<[NetworkSerializationGenerator] TValue>(TKey key, out TValue value)
        {
            if (cache.TryGetValue(key, out var instance))
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
                try
                {
                    using (NetworkReader.Pool.Lease(out var stream))
                    {
                        stream.Assign(raw);
                        value = stream.Read<TValue>();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Exception when Reading key {key} as {typeof(TValue)}, Wrong Type Read Most Likely\n" +
                        $"Exception: {ex}");
                }

                cache[key] = value;

                return true;
            }

            value = default;
            return false;
        }

        public bool Contains(TKey key) => payload.ContainsKey(key);

        public bool Remove(TKey key)
        {
            cache.Remove(key);

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
                cache.Remove(key);
                payload[key] = collection.payload[key];
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            NetworkSerializationHelper.Length.Write(writer, payload.Count);

            foreach (var pair in payload)
            {
                writer.Write(pair.Key);

                NetworkSerializationHelper.Length.Write(writer, pair.Value.Length);
                writer.Insert(pair.Value);
            }
        }
        public void Deserialize(NetworkReader reader)
        {
            NetworkSerializationHelper.Length.Read(reader, out var count);

            payload = new Dictionary<TKey, byte[]>(count);

            for (int i = 0; i < count; i++)
            {
                var key = reader.Read<TKey>();

                var length = NetworkSerializationHelper.Length.Read(reader);
                var raw = reader.TakeArray(length);

                payload.Add(key, raw);
            }
        }

        public NetworkGenericDictionary()
        {
            payload = new Dictionary<TKey, byte[]>();
            cache = new Dictionary<TKey, object>();
        }
    }
}