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
    public static class NetworkPayload
    {
        public const short MinCode = 400;

        public static Dictionary<ushort, Type> Types { get; private set; }

        public static Dictionary<Type, ushort> Codes { get; private set; }

        public static Type GetType(ushort code)
        {
            if (Types.TryGetValue(code, out var type))
                return type;
            else
                throw new Exception($"No NetworkPayload Registerd With Code {code}");
        }

        public static ushort GetCode<T>() => GetCode(typeof(T));
        public static ushort GetCode(object instance) => GetCode(instance.GetType());
        public static ushort GetCode(Type type)
        {
            if (Codes.TryGetValue(type, out var code))
                return code;
            else
                throw new Exception($"Type {type} Not Registered as NetworkPayload");
        }

        #region Register
        public static void Register<T>(ushort code) => Register(code, typeof(T));
        public static void Register(ushort code, Type type)
        {
            ValidateDuplicate(code, type);

            Types.Add(code, type);
            Codes.Add(type, code);
        }

        static void RegisterInternal()
        {
            Register<byte>(0);

            Register<short>(1);
            Register<ushort>(2);

            Register<int>(3);
            Register<uint>(4);

            Register<float>(5);

            Register<bool>(6);

            Register<string>(7);

            Register<Guid>(8);
            Register<DateTime>(9);

            Register<CreateRoomRequest>(10);

            Register<RegisterClientRequest>(11);
            Register<RegisterClientResponse>(12);

            Register<ReadyClientRequest>(13);
            Register<ReadyClientResponse>(14);

            Register<SpawnEntityRequest>(15);
            Register<SpawnEntityCommand>(16);

            Register<DestroyEntityRequest>(17);
            Register<DestroyEntityCommand>(18);

            Register<ClientConnectedPayload>(19);
            Register<ClientDisconnectPayload>(20);

            Register<LobbyInfo>(21);

            Register<RoomBasicInfo>(22);
            Register<RoomInternalInfo>(23);

            Register<NetworkClientInfo>(24);

            Register<NetworkClientProfile>(25);

            Register<RpcRequest>(26);
            Register<RpcCommand>(27);

            Register<AttributesCollection>(28);

            Register<RpcBufferMode>(29);
        }
        #endregion

        #region Validate
        static void ValidateDuplicate(ushort code, Type type)
        {
            ValidateTypeDuplicate(code, type);

            ValidateCodeDuplicate(code, type);
        }

        static void ValidateTypeDuplicate(ushort code, Type type)
        {
            if (Types.TryGetValue(code, out var duplicate))
                throw new Exception($"NetworkPayload Type Duplicate Found, {type} & {duplicate} both registered with code {code}");
        }

        static void ValidateCodeDuplicate(ushort code, Type type)
        {
            if (Codes.TryGetValue(type, out var duplicate))
                throw new Exception($"NetworkPayload Type Duplicate Found, Code {code} & {duplicate} Both Registered to {type}");
        }
        #endregion

        static NetworkPayload()
        {
            Types = new Dictionary<ushort, Type>();

            Codes = new Dictionary<Type, ushort>();

            RegisterInternal();
        }
    }

    [Serializable]
    public sealed class CreateRoomRequest : INetworkSerializable
    {
        string name;
        public string Name { get { return name; } }

        ushort capacity;
        public ushort Capacity { get { return capacity; } }

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(name);
            writer.Write(capacity);
            writer.Write(attributes);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out name);
            reader.Read(out capacity);
            reader.Read(out attributes);
        }

        public CreateRoomRequest() { }
        public CreateRoomRequest(string name, ushort capacity) : this(name, capacity, null) { }
        public CreateRoomRequest(string name, ushort capacity, AttributesCollection attributes)
        {
            this.name = name;
            this.capacity = capacity;
            this.attributes = attributes;
        }
    }

    #region Register Client
    [Serializable]
    public sealed class RegisterClientRequest : INetworkSerializable
    {
        NetworkClientProfile profile;
        public NetworkClientProfile Profile => profile;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(profile);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out profile);
        }

        public RegisterClientRequest() { }
        public RegisterClientRequest(NetworkClientProfile profile)
        {
            this.profile = profile;
        }
    }

    [Serializable]
    public sealed class RegisterClientResponse : INetworkSerializable
    {
        NetworkClientID id;
        public NetworkClientID ID => id;

        RoomInternalInfo room;
        public RoomInternalInfo Room => room;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(id);
            writer.Write(room);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out id);
            reader.Read(out room);
        }

        public RegisterClientResponse() { }
        public RegisterClientResponse(NetworkClientID id, RoomInternalInfo room)
        {
            this.id = id;
            this.room = room;
        }
    }
    #endregion

    #region Ready Client
    [Serializable]
    public sealed class ReadyClientRequest : INetworkSerializable
    {
        public void Serialize(NetworkWriter writer)
        {

        }
        public void Deserialize(NetworkReader reader)
        {

        }

        public ReadyClientRequest() { }
    }

    [Serializable]
    public sealed class ReadyClientResponse : INetworkSerializable
    {
        NetworkClientInfo[] clients;
        public NetworkClientInfo[] Clients => clients;

        List<NetworkMessage> buffer;
        public List<NetworkMessage> Buffer => buffer;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(clients);
            writer.Write(buffer);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out clients);
            reader.Read(out buffer);
        }

        public ReadyClientResponse() { }
        public ReadyClientResponse(NetworkClientInfo[] clients, List<NetworkMessage> buffer)
        {
            this.clients = clients;
            this.buffer = buffer;
        }
    }
    #endregion

    #region Spawn Entity
    [Serializable]
    public sealed class SpawnEntityRequest : INetworkSerializable
    {
        string resource;
        public string Resource { get { return resource; } }

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(resource);
            writer.Write(attributes);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out resource);
            reader.Read(out attributes);
        }

        public SpawnEntityRequest() { }
        public SpawnEntityRequest(string resource) : this(resource, new AttributesCollection()) { }
        public SpawnEntityRequest(string resource, AttributesCollection attributes)
        {
            this.resource = resource;
            this.attributes = attributes;
        }
    }

    [Serializable]
    public sealed class SpawnEntityCommand : INetworkSerializable
    {
        NetworkClientID owner;
        public NetworkClientID Owner { get { return owner; } }

        NetworkEntityID entity;
        public NetworkEntityID Entity { get { return entity; } }

        string resource;
        public string Resource { get { return resource; } }

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(owner);
            writer.Write(entity);
            writer.Write(resource);
            writer.Write(attributes);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out owner);
            reader.Read(out entity);
            reader.Read(out resource);
            reader.Read(out attributes);
        }

        public SpawnEntityCommand() { }
        public SpawnEntityCommand(NetworkClientID owner, NetworkEntityID entity, string resource, AttributesCollection attributes)
        {
            this.owner = owner;
            this.entity = entity;
            this.resource = resource;
            this.attributes = attributes;
        }
    }
    #endregion

    #region Destroy Entity
    [Serializable]
    public sealed class DestroyEntityRequest : INetworkSerializable
    {
        NetworkBehaviourID id;
        public NetworkBehaviourID ID => id;

        public void Deserialize(NetworkReader reader)
        {

        }
        public void Serialize(NetworkWriter writer)
        {

        }

        public DestroyEntityRequest() { }
        public DestroyEntityRequest(NetworkBehaviourID id)
        {
            this.id = id;
        }
    }

    [Serializable]
    public sealed class DestroyEntityCommand : INetworkSerializable
    {
        NetworkEntityID id;
        public NetworkEntityID ID => id;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(id);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out id);
        }

        public DestroyEntityCommand() { }
        public DestroyEntityCommand(NetworkEntityID id)
        {
            this.id = id;
        }
    }
    #endregion

    #region Connection
    [Serializable]
    public sealed class ClientConnectedPayload : INetworkSerializable
    {
        NetworkClientID id;
        public NetworkClientID ID => id;

        NetworkClientProfile profile;
        public NetworkClientProfile Profile => profile;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(id);
            writer.Write(profile);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out id);
            reader.Read(out profile);
        }

        public ClientConnectedPayload() { }
        public ClientConnectedPayload(NetworkClientID id, NetworkClientProfile profile)
        {
            this.id = id;
            this.profile = profile;
        }
    }

    [Serializable]
    public sealed class ClientDisconnectPayload : INetworkSerializable
    {
        NetworkClientID id;
        public NetworkClientID ID => id;

        NetworkClientProfile profile;
        public NetworkClientProfile Profile => profile;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(id);
            writer.Write(profile);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out id);
            reader.Read(out profile);
        }

        public ClientDisconnectPayload() { }
        public ClientDisconnectPayload(NetworkClientID id, NetworkClientProfile profile)
        {
            this.id = id;
            this.profile = profile;
        }
    }
    #endregion
}