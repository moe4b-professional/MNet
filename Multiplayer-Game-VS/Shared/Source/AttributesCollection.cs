using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    public class AttributesCollection : INetworkSerializable
    {
        Dictionary<string, byte[]> payload;

        Dictionary<string, object> objects;

        public const int DefaultValueBufferSize = 256;

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
            if (TryGetValue(key, out object obj))
            {
                try
                {
                    value = (T)obj;

                    return true;
                }
                catch (InvalidCastException)
                {
                    throw new InvalidCastException($"Error Casting {obj.GetType()} to {typeof(T)}");
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                value = default(T);

                return false;
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(payload);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out payload);

            Log.Info(payload.Count);

            objects = new Dictionary<string, object>();
        }

        public AttributesCollection()
        {
            payload = new Dictionary<string, byte[]>();

            objects = new Dictionary<string, object>();
        }
    }
}