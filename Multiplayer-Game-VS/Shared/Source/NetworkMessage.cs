using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;

namespace Game.Shared
{
    [Serializable]
    public sealed class NetworkMessage : INetworkSerializable
    {
        private ushort code;
        public ushort Code { get { return code; } }

        private byte[] raw;
        public byte[] Raw { get { return raw; } }

        public Type Type => NetworkPayload.GetType(code);

        public bool Is<TType>() => NetworkPayload.GetCode<TType>() == code;
        public bool Is(Type type) => NetworkPayload.GetCode(type) == code;

        #region Read
        object payload = null;

        public object Read()
        {
            if (payload == null) payload = NetworkSerializer.Deserialize(raw, Type);

            return payload;
        }

        public T Read<T>()
        {
            var instance = Read();

            try
            {
                return (T)instance;
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
        #endregion

        public void WriteTo(HttpListenerResponse response)
        {
            response.StatusCode = (int)HttpStatusCode.OK;

            var data = NetworkSerializer.Serialize(this);

            response.WriteContent(data);

            response.Close();
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(code);
            writer.Write(raw);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out code);
            reader.Read(out raw);
        }

        public NetworkMessage() { }
        public NetworkMessage(ushort id, byte[] payload)
        {
            this.code = id;

            this.raw = payload;
        }

        public static NetworkMessage Read(byte[] data)
        {
            return NetworkSerializer.Deserialize<NetworkMessage>(data);
        }
        public static NetworkMessage Read(HttpListenerRequest request)
        {
            using (var stream = new MemoryStream())
            {
                request.InputStream.CopyTo(stream);

                var binary = stream.ToArray();

                return Read(binary);
            }
        }

        public static NetworkMessage Write<T>(T payload)
        {
            var code = NetworkPayload.GetCode<T>();

            var raw = NetworkSerializer.Serialize(payload);

            return new NetworkMessage(code, raw);
        }
    }
}