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
    public struct NetworkMessage : IManualNetworkSerializable
    {
        object payload;
        public object Payload => payload;

        public Type Type => payload.GetType();

        public bool Is<TType>()
        {
            var type = typeof(TType);

            return Is(type);
        }
        public bool Is(Type target)
        {
            return target == Type;
        }

        public T Read<T>()
        {
            if (payload is T result)
                return result;
            else
                throw new InvalidCastException($"Trying to read {Type} as {typeof(T)}");
        }
        public bool TryRead<T>(out T output)
            where T : new()
        {
            if (payload is T result)
            {
                output = result;
                return true;
            }
            else
            {
                output = default;
                return false;
            }
        }

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

        NetworkMessage(object payload)
        {
            this.payload = payload;
        }

        //Static Utility

        public static NetworkMessage Write<T>(T payload)
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