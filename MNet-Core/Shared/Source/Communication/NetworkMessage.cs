using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;

namespace MNet
{
    [Preserve]
    [Serializable]
    public class NetworkMessage
    {
        Type type;
        public Type Type => type;

        byte[] raw = default;
        public byte[] Raw => raw;

        public int Size => raw.Length;

        public bool Is<TType>()
        {
            var type = typeof(TType);

            return Is(type);
        }
        public bool Is(Type target)
        {
            return target == Type;
        }

        public T Read<T>() => NetworkSerializer.Deserialize<T>(raw);

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(type);

            var length = (ushort)raw.Length;

            writer.Write(length);
            writer.Insert(raw);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out type);

            reader.Read(out ushort length);
            raw = reader.BlockCopy(length);
        }

        public override string ToString() => type.FullName;

        public NetworkMessage() { }
        NetworkMessage(Type type, byte[] raw)
        {
            this.type = type;
            this.raw = raw;
        }

        //Static Utility

        public static NetworkMessage Write<T>(ref T payload)
        {
            var type = typeof(T);

            var raw = NetworkSerializer.Serialize(payload);

            var message = new NetworkMessage(type, raw);

            return message;
        }

        public static NetworkMessage Read(byte[] data) => NetworkSerializer.Deserialize<NetworkMessage>(data);

        public static IEnumerable<NetworkMessage> ReadAll(byte[] data)
        {
            var reader = new NetworkReader(data);

            while (reader.Remaining > 0)
            {
                var message = reader.Read<NetworkMessage>();

                yield return message;
            }
        }
    }
}