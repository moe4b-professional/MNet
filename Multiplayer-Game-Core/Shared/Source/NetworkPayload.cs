﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;

namespace Backend
{
    public static class NetworkPayload
    {
        public const ushort MinCode = 400;

        public static Dictionary<ushort, Type> Types { get; private set; }

        public static Dictionary<Type, ushort> Codes { get; private set; }

        public static Type GetType(ushort code)
        {
            if (TryGetType(code, out var type))
                return type;
            else
                throw new Exception($"No NetworkPayload Registerd With Code {code}");
        }
        public static bool TryGetType(ushort code, out Type type)
        {
            return Types.TryGetValue(code, out type);
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
            Validate(code, type);

            Types.Add(code, type);
            Codes.Add(type, code);
        }
        #endregion

        #region Validate
        static void Validate(ushort code, Type type)
        {
            ValidateDuplicate(code, type);
        }

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

            Register<ChangeMasterCommand>(32);

            Register<RprRequest>(35);
            Register<RprCommand>(36);
        }

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

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref name);
            context.Select(ref capacity);
            context.Select(ref attributes);
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

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref profile);
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

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
            context.Select(ref room);
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
        public void Select(INetworkSerializableResolver.Context context)
        {

        }

        public ReadyClientRequest() { }
    }

    [Serializable]
    public sealed class ReadyClientResponse : INetworkSerializable
    {
        NetworkClientInfo[] clients;
        public NetworkClientInfo[] Clients => clients;

        NetworkClientID master;
        public NetworkClientID Master => master;

        List<NetworkMessage> buffer;
        public List<NetworkMessage> Buffer => buffer;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref clients);
            context.Select(ref buffer);
            context.Select(ref master);
        }

        public ReadyClientResponse() { }
        public ReadyClientResponse(NetworkClientInfo[] clients, NetworkClientID master, List<NetworkMessage> buffer)
        {
            this.clients = clients;
            this.master = master;
            this.buffer = buffer;
        }
    }
    #endregion

    #region Spawn Entity
    [Serializable]
    public sealed class SpawnEntityRequest : INetworkSerializable
    {
        NetworkEntityType type;
        public NetworkEntityType Type => type;

        string resource;
        public string Resource { get { return resource; } }

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        int scene;
        public int Scene => scene;

        int index;
        public int Index => index;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref type);

            switch (type)
            {
                case NetworkEntityType.Dynamic:
                    context.Select(ref resource);
                    context.Select(ref attributes);
                    break;

                case NetworkEntityType.SceneObject:
                    context.Select(ref scene);
                    context.Select(ref index);
                    break;
            }
        }

        public SpawnEntityRequest() { }

        public static SpawnEntityRequest Write(string resource) => Write(resource, null);
        public static SpawnEntityRequest Write(string resource, AttributesCollection attributes)
        {
            var request = new SpawnEntityRequest()
            {
                type = NetworkEntityType.Dynamic,
                resource = resource,
                attributes = attributes,
            };

            return request;
        }

        public static SpawnEntityRequest Write(int scene, int index)
        {
            var request = new SpawnEntityRequest()
            {
                type = NetworkEntityType.SceneObject,
                scene = scene,
                index = index,
            };

            return request;
        }
    }

    [Serializable]
    public sealed class SpawnEntityCommand : INetworkSerializable
    {
        NetworkClientID owner;
        public NetworkClientID Owner { get { return owner; } }

        NetworkEntityID id;
        public NetworkEntityID ID { get { return id; } }

        NetworkEntityType type;
        public NetworkEntityType Type => type;

        string resource;
        public string Resource { get { return resource; } }

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        int scene;
        public int Scene => scene;

        int index;
        public int Index => index;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref owner);
            context.Select(ref id);

            context.Select(ref type);

            switch (type)
            {
                case NetworkEntityType.Dynamic:
                    context.Select(ref resource);
                    context.Select(ref attributes);
                    break;

                case NetworkEntityType.SceneObject:
                    context.Select(ref scene);
                    context.Select(ref index);
                    break;
            }
        }

        public SpawnEntityCommand() { }

        public static SpawnEntityCommand Write(NetworkClientID owner, NetworkEntityID id, SpawnEntityRequest request)
        {
            var command = new SpawnEntityCommand()
            {
                owner = owner,
                id = id,
                type = request.Type,
                resource = request.Resource,
                attributes = request.Attributes,
                scene = request.Scene,
                index = request.Index,
            };

            return command;
        }
    }
    #endregion

    #region Destroy Entity
    [Serializable]
    public sealed class DestroyEntityRequest : INetworkSerializable
    {
        NetworkEntityID id;
        public NetworkEntityID ID => id;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
        }

        public DestroyEntityRequest() { }
        public DestroyEntityRequest(NetworkEntityID id)
        {
            this.id = id;
        }
    }

    [Serializable]
    public sealed class DestroyEntityCommand : INetworkSerializable
    {
        NetworkEntityID id;
        public NetworkEntityID ID => id;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
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
        NetworkClientInfo info;
        public NetworkClientInfo Info => info;

        public NetworkClientID ID => info.ID;

        public NetworkClientProfile Profile => info.Profile;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref info);
        }

        public ClientConnectedPayload() { }
        public ClientConnectedPayload(NetworkClientInfo info)
        {
            this.info = info;
        }
    }

    [Serializable]
    public sealed class ClientDisconnectPayload : INetworkSerializable
    {
        NetworkClientID id;
        public NetworkClientID ID => id;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
        }

        public ClientDisconnectPayload() { }
        public ClientDisconnectPayload(NetworkClientID id)
        {
            this.id = id;
        }
    }
    #endregion

    public sealed class ChangeMasterCommand : INetworkSerializable
    {
        NetworkClientID id;
        public NetworkClientID ID => id;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
        }

        public ChangeMasterCommand() { }
        public ChangeMasterCommand(NetworkClientID id)
        {
            this.id = id;
        }
    }
}