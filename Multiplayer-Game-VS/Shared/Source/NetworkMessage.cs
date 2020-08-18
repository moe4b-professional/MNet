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
        private short code;
        public short Code { get { return code; } }

        private byte[] raw;
        public byte[] Raw { get { return raw; } }

        public bool Is<TType>()
            where TType : NetworkMessagePayload
        {
            return NetworkMessagePayload.GetCode<TType>() == code;
        }
        public bool Is(Type type)
        {
            return NetworkMessagePayload.GetCode(type) == code;
        }

        public object Read()
        {
            var type = NetworkMessagePayload.GetType(Code);

            var instance = NetworkSerializer.Deserialize(raw, type);

            return instance;
        }
        public TType Read<TType>()
            where TType : NetworkMessagePayload, new()
        {
            var instance = NetworkSerializer.Deserialize<TType>(raw);

            return instance;
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
        public NetworkMessage(short id, byte[] payload)
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

                var binary = stream.ToArray(); ;

                return Read(binary);
            }
        }

        public static NetworkMessage Write(NetworkMessagePayload payload)
        {
            var code = NetworkMessagePayload.GetCode(payload.Type);

            var raw = NetworkSerializer.Serialize(payload);

            return new NetworkMessage(code, raw);
        }
    }

    [Serializable]
    public abstract class NetworkMessagePayload : INetSerializable
    {
        public Type Type => GetType();

        public NetworkMessage ToMessage() => NetworkMessage.Write(this);

        public abstract void Serialize(NetworkWriter writer);
        public abstract void Deserialize(NetworkReader reader);

        public NetworkMessagePayload() { }

        //Static Utility
        static Type Target => typeof(NetworkMessagePayload);

        public static List<Data> All { get; private set; }
        public class Data
        {
            public short Code { get; protected set; }

            public Type Type { get; protected set; }

            public Data(short code, Type type)
            {
                this.Code = code;
                this.Type = type;
            }
        }

        public static Type GetType(short code)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Code == code)
                    return All[i].Type;

            return null;
        }

        public static short GetCode<T>() where T : NetworkMessagePayload => GetCode(typeof(T));
        public static short GetCode(Type type)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Type == type)
                    return All[i].Code;

            throw new NotImplementedException($"Type {type.Name} not registerd as {nameof(NetworkMessagePayload)}");
        }

        static void Register<T>(short value) where T : NetworkMessagePayload => Register(value, typeof(T));
        static void Register(short value, Type type)
        {
            var element = new Data(value, type);

            if (type == Target)
                throw new InvalidOperationException($"Cannot register {nameof(NetworkMessagePayload)} directly, please inherit from it");

            if (Target.IsAssignableFrom(type) == false)
                throw new InvalidOperationException($"Cannot register type {type.Name} as {nameof(NetworkMessagePayload)} as it's not a sub-class");

            if (type.BaseType != Target)
                throw new Exception($"{type.Name} must inherit from {Target.Name} directly!");

            var constructor = type.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
                throw new Exception($"{type.FullName} rquires a parameter-less constructor to act as a {nameof(NetworkMessagePayload)}");

            All.Add(element);
        }

        static NetworkMessagePayload()
        {
            All = new List<Data>();

            Register<RoomListInfoPayload>(1);
            Register<PlayerInfoPayload>(2);
            Register<RPCPayload>(3);
            Register<CreateRoomPayload>(4);
            Register<RoomInfoPayload>(5);
        }
    }

    [Serializable]
    public sealed class RoomListInfoPayload : NetworkMessagePayload
    {
        private RoomInfo[] list;
        public RoomInfo[] List { get { return list; } }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(list);
        }

        public override void Deserialize(NetworkReader reader)
        {
            reader.Read(out list);
        }

        public RoomListInfoPayload() { }
        public RoomListInfoPayload(RoomInfo[] list)
        {
            this.list = list;
        }
    }

    [Serializable]
    public sealed class PlayerInfoPayload : NetworkMessagePayload
    {
        private Dictionary<string, string> dictionary;
        public Dictionary<string, string> Dictionary { get { return dictionary; } }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(dictionary);
        }

        public override void Deserialize(NetworkReader reader)
        {
            reader.Read(out dictionary);
        }

        public PlayerInfoPayload() { }
        public PlayerInfoPayload(Dictionary<string, string> dictionary)
        {
            this.dictionary = dictionary;
        }
    }

    [Serializable]
    public sealed class RPCPayload : NetworkMessagePayload
    {
        private string target;
        public string Target { get { return target; } }

        private byte[] raw;
        public byte[] Raw { get { return raw; } }

        public object[] Read(IList<ParameterInfo> parameters)
        {
            var results = new object[parameters.Count];

            var reader = new NetworkReader(raw);

            for (int i = 0; i < parameters.Count; i++)
            {
                var value = reader.Read(parameters[i].ParameterType);

                results[i] = value;
            }

            return results;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(target);
            writer.Write(raw);
        }
        public override void Deserialize(NetworkReader reader)
        {
            reader.Read(out target);
            reader.Read(out raw);
        }

        public RPCPayload() { }
        public RPCPayload(string target, byte[] raw)
        {
            this.target = target;

            this.raw = raw;
        }

        public static RPCPayload Write(string target, params object[] arguments)
        {
            var writer = new NetworkWriter(1024);

            for (int i = 0; i < arguments.Length; i++)
                writer.Write(arguments[i]);

            var raw = writer.Read();

            var payload = new RPCPayload(target, raw);

            return payload;
        }
    }

    [Serializable]
    public sealed class CreateRoomPayload : NetworkMessagePayload
    {
        private string name;
        public string Name { get { return name; } }

        private short capacity;
        public short Capacity { get { return capacity; } }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(name);
            writer.Write(capacity);
        }

        public override void Deserialize(NetworkReader reader)
        {
            reader.Read(out name);
            reader.Read(out capacity);
        }

        public CreateRoomPayload() { }
        public CreateRoomPayload(string name, short capacity)
        {
            this.name = name;
            this.capacity = capacity;
        }
    }

    [Serializable]
    public sealed class RoomInfoPayload : NetworkMessagePayload
    {
        private RoomInfo info;
        public RoomInfo Info { get { return info; } }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(info);
        }

        public override void Deserialize(NetworkReader reader)
        {
            reader.Read(out info);
        }

        public RoomInfoPayload() { }
        public RoomInfoPayload(RoomInfo info)
        {
            this.info = info;
        }
    }
}