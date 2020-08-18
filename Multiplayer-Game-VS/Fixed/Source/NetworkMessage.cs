using System;
using System.Collections.Generic;
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
            return NetworkMessagePayload.Collection.GetCode<TType>() == code;
        }
        public bool Is(Type type)
        {
            return NetworkMessagePayload.Collection.GetCode(type) == code;
        }

        public object Read()
        {
            var type = NetworkMessagePayload.Collection.GetType(Code);

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

        public static NetworkMessage Write<T>(T payload)
            where T : NetworkMessagePayload
        {
            var code = NetworkMessagePayload.Collection.GetCode<T>();

            var raw = NetworkSerializer.Serialize(payload);

            return new NetworkMessage(code, raw);
        }
    }

    [Serializable]
    public abstract class NetworkMessagePayload : INetSerializable
    {
        public static class Collection
        {
            public static Type Target => typeof(NetworkMessagePayload);

            public static List<Element> List { get; private set; }
            public class Element
            {
                public short Code { get; protected set; }

                public Type Type { get; protected set; }

                public Element(short code, Type type)
                {
                    this.Code = code;
                    this.Type = type;
                }
            }

            public static Type GetType(short code)
            {
                for (int i = 0; i < List.Count; i++)
                    if (List[i].Code == code)
                        return List[i].Type;

                return null;
            }

            public static short GetCode<T>() where T : NetworkMessagePayload => GetCode(typeof(T));
            public static short GetCode(Type type)
            {
                for (int i = 0; i < List.Count; i++)
                    if (List[i].Type == type)
                        return List[i].Code;

                Log.Info(type.FullName);

                throw new NotImplementedException();
            }

            static void Register<T>(short value) where T : NetworkMessagePayload => Register(value, typeof(T));
            static void Register(short value, Type type)
            {
                var element = new Element(value, type);

                if (type == Target)
                    throw new InvalidOperationException($"Cannot register {nameof(NetworkMessagePayload)} directly, please inherit from it");

                if (Target.IsAssignableFrom(type) == false)
                    throw new InvalidOperationException($"Cannot register type {type.Name} as {nameof(NetworkMessagePayload)} as it's not a sub-class");

                if (type.BaseType != Target)
                    throw new Exception($"{type.Name} must inherit from {Target.Name} directly!");

                var constructor = type.GetConstructor(Type.EmptyTypes);

                if (constructor == null)
                    throw new Exception($"{type.FullName} rquires a parameter-less constructor to act as a {nameof(NetworkMessagePayload)}");

                List.Add(element);
            }

            static Collection()
            {
                List = new List<Element>();

                Register<ListRoomsPayload>(1);
                Register<PlayerInfoPayload>(2);
                Register<RPCPayload>(3);
            }
        }
        
        public abstract void Deserialize(NetworkReader reader);

        public abstract void Serialize(NetworkWriter writer);
    }

    [Serializable]
    public sealed class ListRoomsPayload : NetworkMessagePayload
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

        public ListRoomsPayload() { }
        public ListRoomsPayload(RoomInfo[] list)
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
}