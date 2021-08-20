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
    public struct NetworkMessage
    {
        object payload;
        public object Payload => payload;

        public Type Type => payload.GetType();

        public bool Is<TType>() => payload is TType;

        public T Read<T>() => (T)payload;

        public BufferNetworkMessage ToBuffer() => new BufferNetworkMessage(payload);

        public void Serialize(NetworkStream writer)
        {
            writer.Write(Type);
            writer.Write(payload);
        }
        public void Deserialize(NetworkStream reader)
        {
            reader.Read(out Type type);
            payload = reader.Read(type);
        }

        public override string ToString() => payload.ToString();

        public NetworkMessage(object payload)
        {
            this.payload = payload;
        }

        //Static Utility

        public static NetworkMessage Write<T>(ref T payload)
        {
            var message = new NetworkMessage(payload);

            return message;
        }

        //TODO: Stop using iterator for performance reasons
        public static IEnumerable<NetworkMessage> Read(ArraySegment<byte> segment)
        {
            var stream = new NetworkStream(segment.Array, segment.Offset);

            var end = segment.Offset + segment.Count;

            while (true)
            {
                var message = stream.Read<NetworkMessage>();

                yield return message;

                if (stream.Position == end) break;

                if(stream.Position > end)
                {
                    Log.Error($"Network Message Stream Read Went Past the Expected Length! Are you Sure You are Passing a valid ArraySegment?");
                    break;
                }
            }
        }
    }

    public class BufferNetworkMessage
    {
        object payload;
        public object Payload => payload;

        public Type Type => payload.GetType();

        public bool Is<TType>() => payload is TType;

        public T Read<T>() => (T)payload;

        public void Set<T>(T target) => payload = target;

        public NetworkMessage ToMessage() => new NetworkMessage(payload);

        public BufferNetworkMessage(object payload)
        {
            this.payload = payload;
        }
    }
}