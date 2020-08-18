using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;

namespace Game.Shared
{
    [Serializable]
    public sealed class NetworkMessage : INetSerializable
    {
        private string id;
        public string ID { get { return id; } }

        private byte[] raw;
        public byte[] Raw { get { return raw; } }

        public bool Is<TType>()
            where TType : NetworkMessagePayload
        {
            return NetworkMessagePayload.GetID<TType>() == id;
        }
        public bool Is(Type type)
        {
            return NetworkMessagePayload.GetID(type) == id;
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
            writer.Write(id);
            writer.Write(raw);
        }

        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out id);
            reader.Read(out raw);
        }

        public NetworkMessage() { }
        public NetworkMessage(string id, byte[] payload)
        {
            this.id = id;

            this.raw = payload;
        }

        public static NetworkMessage Read(byte[] data)
        {
            return NetworkSerializer.Deserialize<NetworkMessage>(data);
        }

        public static NetworkMessage Write<TPayload>(TPayload payload)
            where TPayload : NetworkMessagePayload
        {
            var id = NetworkMessagePayload.GetID(payload);

            var data = NetworkSerializer.Serialize(payload);

            return new NetworkMessage(id, data);
        }
    }

    [Serializable]
    public abstract class NetworkMessagePayload : INetSerializable
    {
        public static string GetID<TPayload>() where TPayload : NetworkMessagePayload => GetID(typeof(TPayload));
        public static string GetID(NetworkMessagePayload payload) => GetID(payload.GetType());
        public static string GetID(Type type) => type.Name;
        
        public static void ValidateAll()
        {
            var target = typeof(NetworkMessagePayload);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type == target) continue; //Don't register NetworkMessagePayloadAttribute it's self
                    
                    if (target.IsAssignableFrom(type) == false) continue;

                    if (type.BaseType != target)
                        throw new Exception($"{type.Name} must inherit from {target.Name} directly!");

                    var constructor = type.GetConstructor(Type.EmptyTypes);

                    if (constructor == null)
                        throw new Exception($"{type.FullName} rquires a parameter-less constructor to act as a {nameof(NetworkMessagePayload)}");
                }
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
    public sealed class RPCInfoPayload : NetworkMessagePayload
    {
        private string target;
        public string Target { get { return target; } }

        private RPCArgument[] arguments;
        public RPCArgument[] Arguments { get { return arguments; } }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(target);

            writer.WriteArray(arguments);
        }

        public override void Deserialize(NetworkReader reader)
        {
            reader.Read(out target);

            reader.Read(out arguments);
        }

        public RPCInfoPayload() { }
        public RPCInfoPayload(string target, params RPCArgument[] arguments)
        {
            this.target = target;

            this.arguments = arguments;
        }
    }

    [Serializable]
    public sealed class RPCArgument : INetSerializable
    {
        private string id;
        public string ID { get { return id; } }

        private byte[] raw;
        public byte[] Raw { get { return raw; } }

        public object Read()
        {
            var type = Type.GetType(id);

            var result = NetworkSerializer.Deserialize(raw, type);

            return result;
        }
        public T Read<T>() => (T)Read();

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(id);

            writer.Write(raw);
        }

        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out id);

            reader.Read(out raw);
        }

        public RPCArgument() { }
        public RPCArgument(string id, byte[] raw)
        {
            this.id = id;

            this.raw = raw;
        }

        public static RPCArgument Create(object value)
        {
            var id = value.GetType().AssemblyQualifiedName;

            var data = NetworkSerializer.Serialize(value);

            return new RPCArgument(id, data);
        }
    }
}