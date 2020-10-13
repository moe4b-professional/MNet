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
    [Serializable]
    public sealed class NetworkMessage : INetworkSerializable
    {
        ushort code;
        public ushort Code { get { return code; } }

        byte[] raw;
        public byte[] Raw { get { return raw; } }

        Type _type;
        public Type Type
        {
            get
            {
                if (_type == null) NetworkPayload.TryGetType(code, out _type);

                return _type;
            }
        }

        object _payload;
        public object Payload
        {
            get
            {
                if (_payload == null) _payload = NetworkSerializer.Deserialize(raw, Type);

                return _payload;
            }
        }

        public bool Is<TType>() => Is(typeof(TType));
        public bool Is(Type target) => target.IsAssignableFrom(Type);

        public T Read<T>()
        {
            try
            {
                return (T)Payload;
            }
            catch(InvalidCastException)
            {
                throw new InvalidCastException($"Trying to read {Type} as {typeof(T).Name}");
            }
            catch (Exception)
            {
                throw;
            }
        }
        public bool TryRead<T>(out T payload)
            where T : new()
        {
            if (Is<T>())
            {
                payload = Read<T>();
                return true;
            }
            else
            {
                payload = default(T);
                return false;
            }
        }

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref code);
            context.Select(ref raw);
        }

        public NetworkMessage() { }

        public static NetworkMessage Read(byte[] data)
        {
            return NetworkSerializer.Deserialize<NetworkMessage>(data);
        }

        public static NetworkMessage Write<T>(T payload)
        {
            var code = NetworkPayload.GetCode<T>();
            var raw = NetworkSerializer.Serialize(payload);

            var message = new NetworkMessage()
            {
                code = code,
                raw = raw,
            };

            return message;
        }
    }
}