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
    public sealed class NetworkMessage : INetSerializable
    {
        private ushort code;
        public ushort Code { get { return code; } }

        private byte[] raw;
        public byte[] Raw { get { return raw; } }

        public Type Type => NetworkMessagePayload.GetType(code);

        public bool Is<TType>() => NetworkMessagePayload.GetCode<TType>() == code;
        public bool Is(Type type) => NetworkMessagePayload.GetCode(type) == code;

        public object Read()
        {
            var instance = NetworkSerializer.Deserialize(raw, Type);

            return instance;
        }
        public TType Read<TType>()
            where TType : new()
        {
            var instance = NetworkSerializer.Deserialize<TType>(raw);

            return instance;
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

        public static NetworkMessage Write(object payload)
        {
            var type = payload.GetType();

            var code = NetworkMessagePayload.GetCode(type);

            var raw = NetworkSerializer.Serialize(payload);

            return new NetworkMessage(code, raw);
        }
    }

    public static class NetworkMessagePayload
    {
        public static List<Data> List { get; private set; }
        public class Data
        {
            public ushort Code { get; protected set; }

            public Type Type { get; protected set; }

            public Data(ushort code, Type type)
            {
                this.Code = code;
                this.Type = type;
            }
        }

        public static Type GetType(ushort code)
        {
            for (int i = 0; i < List.Count; i++)
                if (List[i].Code == code)
                    return List[i].Type;

            return null;
        }

        public static ushort GetCode<T>() => GetCode(typeof(T));
        public static ushort GetCode(Type type)
        {
            for (int i = 0; i < List.Count; i++)
                if (List[i].Type == type)
                    return List[i].Code;

            throw new NotImplementedException($"Type {type.Name} not registerd as {nameof(NetworkMessagePayload)}");
        }

        static void Register<T>(ushort value) => Register(value, typeof(T));
        static void Register(ushort value, Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
                throw new Exception($"{type.FullName} rquires a parameter-less constructor to act as a {nameof(NetworkMessagePayload)}");

            var element = new Data(value, type);

            List.Add(element);
        }

        static NetworkMessagePayload()
        {
            List = new List<Data>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attribute = type.GetCustomAttribute<NetworkMessagePayloadAttribute>();

                    if (attribute == null) continue;

                    Register(attribute.Code, type);
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class NetworkMessagePayloadAttribute : Attribute
    {
        public ushort Code { get; private set; }

        public NetworkMessagePayloadAttribute(ushort code)
        {
            this.Code = code;
        }
    }
    
    [Serializable]
    [NetworkMessagePayload(4)]
    public sealed class CreateRoomRequest : INetSerializable
    {
        string name;
        public string Name { get { return name; } }

        ushort capacity;
        public ushort Capacity { get { return capacity; } }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(name);
            writer.Write(capacity);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out name);
            reader.Read(out capacity);
        }

        public CreateRoomRequest() { }
        public CreateRoomRequest(string name, ushort capacity)
        {
            this.name = name;
            this.capacity = capacity;
        }
    }

    [Serializable]
    [NetworkMessagePayload(5)]
    public sealed class SpawnEntityRequest : INetSerializable
    {
        string resource;
        public string Resource { get { return resource; } }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(resource);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out resource);
        }

        public SpawnEntityRequest() { }
        public SpawnEntityRequest(string resourcePath)
        {
            this.resource = resourcePath;
        }
    }

    [Serializable]
    [NetworkMessagePayload(6)]
    public sealed class SpawnEntityCommand : INetSerializable
    {
        NetworkClientID owner;
        public NetworkClientID Owner { get { return owner; } }

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        string resource;
        public string Resource { get { return resource; } }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(owner);
            writer.Write(entity);
            writer.Write(resource);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out owner);
            reader.Read(out entity);
            reader.Read(out resource);
        }

        public SpawnEntityCommand() { }
        
        public static SpawnEntityCommand Write(NetworkClientID owner, NetworkEntityID entity, string resource)
        {
            var request = new SpawnEntityCommand()
            {
                owner = owner,
                entity = entity,
                resource = resource
            };

            return request;
        }
    }

    [Serializable]
    [NetworkMessagePayload(7)]
    public sealed class ReadyClientRequest : INetSerializable
    {
        ClientInfo info;
        public ClientInfo Info => info;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(info);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out info);
        }

        public ReadyClientRequest() { }
        public ReadyClientRequest(ClientInfo info)
        {
            this.info = info;
        }
    }

    [Serializable]
    [NetworkMessagePayload(8)]
    public sealed class ReadyClientResponse : INetSerializable
    {
        NetworkClientID clientID;
        public NetworkClientID ClientID => clientID;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(clientID);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out clientID);
        }

        public ReadyClientResponse() { }
        public ReadyClientResponse(NetworkClientID clientID)
        {
            this.clientID = clientID;
        }
    }
}