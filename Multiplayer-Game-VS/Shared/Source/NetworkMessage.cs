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

            var code = NetworkPayload.GetCode(type);

            var raw = NetworkSerializer.Serialize(payload);

            return new NetworkMessage(code, raw);
        }
    }
    
    [Serializable]
    public sealed class CreateRoomRequest : INetworkSerializable
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

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(resource);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out resource);
        }

        public SpawnEntityRequest() { }
        public SpawnEntityRequest(string resource)
        {
            this.resource = resource;
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