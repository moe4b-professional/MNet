using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;

namespace Game.Fixed
{
    [Serializable]
    public sealed class NetworkMessage : INetSerializable
    {
        public string ID { get; private set; }

        public byte[] Raw { get; private set; }

        public bool Is<TType>()
            where TType : NetworkMessagePayload
        {
            return NetworkMessagePayload.GetID<TType>() == ID;
        }
        public bool Is(Type type)
        {
            return NetworkMessagePayload.GetID(type) == ID;
        }

        public TType Read<TType>()
            where TType : NetworkMessagePayload, new()
        {
            var instance = NetworkSerializer.Deserialize<TType>(Raw);

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
            writer.WriteString(ID);

            writer.WriteArray(Raw);
        }

        public void Deserialize(NetworkReader reader)
        {
            ID = reader.ReadString();

            Raw = reader.ReadArray<byte>();
        }

        public NetworkMessage() { }
        public NetworkMessage(string id, byte[] payload)
        {
            this.ID = id;

            this.Raw = payload;
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
        public RoomInfo[] list { get; private set; }

        public override void Serialize(NetworkWriter writer)
        {
            writer.WriteArray(list);
        }

        public override void Deserialize(NetworkReader reader)
        {
            list = reader.ReadArray<RoomInfo>();
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
        public Dictionary<string, string> Dictionary { get; private set; }

        public override void Serialize(NetworkWriter writer)
        {
            writer.WriteDictionary(Dictionary);
        }

        public override void Deserialize(NetworkReader reader)
        {
            Dictionary = reader.ReadDictionary<string, string>();
        }

        public PlayerInfoPayload() { }
        public PlayerInfoPayload(Dictionary<string, string> dictionary)
        {
            this.Dictionary = dictionary;
        }
    }

    [Serializable]
    public sealed class RPCInfoPayload : NetworkMessagePayload
    {
        public string Target { get; private set; }

        public RPCArgument[] Arguments { get; private set; }

        public override void Serialize(NetworkWriter writer)
        {
            writer.WriteString(Target);

            writer.WriteArray(Arguments);
        }

        public override void Deserialize(NetworkReader reader)
        {
            Target = reader.ReadString();

            Arguments = reader.ReadArray<RPCArgument>();
        }

        public RPCInfoPayload() { }
    }

    [Serializable]
    public class RPCArgument : INetSerializable
    {
        public string ID { get; private set; }

        public byte[] Raw { get; private set; }

        public object Read()
        {
            var type = Type.GetType(ID);

            var result = NetworkSerializer.Deserialize(Raw, type);

            return result;
        }
        public T Read<T>() => (T)Read();

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteString(ID);

            writer.WriteArray(Raw);
        }

        public void Deserialize(NetworkReader reader)
        {
            ID = reader.ReadString();

            Raw = reader.ReadArray<byte>();
        }

        public RPCArgument() { }
        public RPCArgument(string id, byte[] raw)
        {
            this.ID = id;

            this.Raw = raw;
        }

        public static RPCArgument Create(object value)
        {
            var id = value.GetType().AssemblyQualifiedName;

            var data = NetworkSerializer.Serialize(value);

            return new RPCArgument(id, data);
        }
    }
}