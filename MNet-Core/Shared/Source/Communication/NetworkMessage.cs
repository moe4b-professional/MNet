using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using WebSocketSharp;
using WebSocketSharp.Net;

namespace MNet
{
    [Preserve]
    [Serializable]
    public class NetworkMessage
    {
        object payload = default;
        public object Payload => payload;

        public Type Type => payload.GetType();

        public bool Is<TType>() => payload is TType;

        public T Read<T>() => (T)payload;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(Type);
            writer.Write(payload);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out Type type);
            payload = reader.Read(type);
        }

        public override string ToString() => payload.ToString();

        public NetworkMessage() { }
        NetworkMessage(object payload)
        {
            this.payload = payload;
        }

        //Static Utility

        public static NetworkMessage Write<T>(ref T payload)
        {
            var message = new NetworkMessage(payload);

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