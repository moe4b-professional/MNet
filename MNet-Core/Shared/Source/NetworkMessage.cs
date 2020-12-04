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
    public struct NetworkMessage : INetworkSerializable
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
            catch(InvalidCastException ex)
            {
                throw new InvalidCastException($"Trying to read {Type} as {typeof(T)}\nInternal Exception: {ex}");
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

        public const int HeaderSize = sizeof(ushort);

        public int BinarySize => raw.Length + HeaderSize;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref code);
            context.Select(ref raw);
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