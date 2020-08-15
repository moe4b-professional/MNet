using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;

using ProtoBuf;

namespace Game.Fixed
{
    [Serializable]
    [ProtoContract]
    public sealed class NetworkMessage
    {
        [ProtoMember(1)]
        public string ID { get; private set; }

        [ProtoMember(2)]
        public byte[] Data { get; private set; }

        public bool Is<TPayload>()
            where TPayload : NetworkMessagePayload
        {
            return NetworkMessagePayload.GetID<TPayload>() == ID;
        }
        public bool Is(Type type)
        {
            return NetworkMessagePayload.GetID(type) == ID;
        }

        public TPayload Read<TPayload>()
            where TPayload : NetworkMessagePayload
        {
            var instance = NetworkSerializer.Deserialize<TPayload>(Data);

            return instance;
        }

        public void WriteTo(HttpListenerResponse response)
        {
            response.StatusCode = (int)HttpStatusCode.OK;

            var data = NetworkSerializer.Serialize(this);

            response.WriteContent(data);

            response.Close();
        }
        
        public NetworkMessage(string id, byte[] payload)
        {
            this.ID = id;

            this.Data = payload;
        }

        public static NetworkMessage Read(byte[] data)
        {
            return NetworkSerializer.Deserialize<NetworkMessage>(data);
        }

        public static NetworkMessage Write<TPayload>(TPayload payload)
            where TPayload : NetworkMessagePayload
        {
            var id = NetworkMessagePayload.GetID(payload);

            var data = NetworkSerializer.Serialize<TPayload>(payload);

            return new NetworkMessage(id, data);
        }

        public NetworkMessage()
        {

        }
    }

    [Serializable]
    [ProtoContract]
    public class NetworkMessagePayload
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
    }

    [Serializable]
    [ProtoContract]
    public sealed class ListRoomsPayload : NetworkMessagePayload
    {
        [ProtoMember(1)]
        public IList<RoomInfo> list { get; private set; }

        public ListRoomsPayload(IList<RoomInfo> list)
        {
            this.list = list;
        }

        public ListRoomsPayload()
        {

        }
    }

    [Serializable]
    [ProtoContract]
    public sealed class PlayerInfoPayload : NetworkMessagePayload
    {
        [ProtoMember(1)]
        public Dictionary<string, string> Dictionary { get; private set; }

        public PlayerInfoPayload(Dictionary<string, string> dictionary)
        {
            this.Dictionary = dictionary;
        }

        public PlayerInfoPayload()
        {

        }
    }

    [Serializable]
    [ProtoContract]
    public sealed class RPCInfoPayload : NetworkMessagePayload
    {
        string target;

        RPCArgument[] arguments;
    }

    [Serializable]
    [ProtoContract]
    public class RPCArgument
    {
        [ProtoMember(1)]
        public string ID;

        [ProtoMember(2)]
        public byte[] data;

        public object Read()
        {
            var type = Type.GetType(ID);

            var result = NetworkSerializer.Deserialize(data, type);

            return result;
        }
        public T Read<T>() => (T)Read();

        public RPCArgument()
        {

        }
        public RPCArgument(string name, byte[] data)
        {
            this.ID = name;

            this.data = data;
        }

        public static RPCArgument Create(object value)
        {
            var id = value.GetType().AssemblyQualifiedName;

            var data = NetworkSerializer.Serialize(value);

            return new RPCArgument(id, data);
        }
    }
}