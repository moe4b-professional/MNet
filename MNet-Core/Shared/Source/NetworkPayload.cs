using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace MNet
{
    public static class NetworkPayload
    {
        public const ushort MinCode = 4000;

        public static Dictionary<ushort, Type> Codes { get; private set; }
        public static Dictionary<Type, ushort> Types { get; private set; }

        public static List<Type> Implicits { get; private set; }

        #region Get Type
        public static Type GetType(ushort code)
        {
            if (TryGetType(code, out var type))
                return type;
            else
                throw new Exception($"No NetworkPayload Registerd With Code {code}");
        }

        public static bool TryGetType(ushort code, out Type type) => Codes.TryGetValue(code, out type);
        #endregion

        #region Get Code
        public static ushort GetCode<T>()
        {
            var type = typeof(T);

            return GetCode(type);
        }
        public static ushort GetCode(object instance)
        {
            if (instance == null) throw new ArgumentNullException();

            var type = instance.GetType();

            return GetCode(type);
        }
        public static ushort GetCode(Type type)
        {
            if (TryGetCode(type, out var code))
                return code;
            else
                throw new Exception($"Type {type} Not Registered as NetworkPayload");
        }

        public static bool TryGetCode(Type type, out ushort code)
        {
            if (Types.TryGetValue(type, out code)) return true;

            if (TryGetCodeFromImplicit(type, out code))
            {
                Types.Add(type, code);
                return true;
            }

            return false;
        }

        public static bool TryGetCodeFromImplicit(Type target, out ushort code)
        {
            for (int i = 0; i < Implicits.Count; i++)
                if (Implicits[i].IsAssignableFrom(target))
                    return TryGetCode(Implicits[i], out code);

            code = 0;
            return false;
        }
        #endregion

        #region Register
        public static void Register<T>(ushort code) => Register<T>(code, false);
        public static void Register<T>(ushort code, bool useForChildern)
        {
            var type = typeof(T);

            Register(code, type, useForChildern);
        }

        public static void Register(ushort code, Type type, bool useForChildern)
        {
            Validate(code, type);

            Codes.Add(code, type);
            Types.Add(type, code);

            if (useForChildern) Implicits.Add(type);
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
            if (TryGetType(code, out var duplicate))
                throw new Exception($"NetworkPayload Type Duplicate Found, '{type}' & '{duplicate}' both registered with code '{code}'");
        }
        static void ValidateCodeDuplicate(ushort code, Type type)
        {
            if (TryGetCode(type, out var duplicate))
                throw new Exception($"NetworkPayload Type Duplicate Found, Code '{code}' & '{duplicate}' Both Registered to '{type}'");
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
            Register<TimeSpan>(10);

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

            Register<RoomBasicInfo>(22);
            Register<RoomInnerInfo>(23);

            Register<NetworkClientInfo>(24);
            Register<NetworkClientProfile>(25);

            Register<RpcRequest>(26);
            Register<RpcCommand>(27);
            Register<RpcBufferMode>(28);

            Register<AttributesCollection>(29);

            Register<ChangeMasterCommand>(32);

            Register<RprRequest>(35);
            Register<RprCommand>(36);

            Register<SyncVarRequest>(37);
            Register<SyncVarCommand>(38);

            Register<long>(39);
            Register<ulong>(40);

            Register<double>(41);

            Register<RoomTimeRequest>(42);
            Register<RoomTimeResponse>(43);

            Register<PingRequest>(44);
            Register<PingResponse>(45);
        }

        static NetworkPayload()
        {
            Codes = new Dictionary<ushort, Type>();
            Types = new Dictionary<Type, ushort>();

            Implicits = new List<Type>();

            RegisterInternal();
        }
    }

    #region REST
    [Preserve]
    [Serializable]
    public struct CreateRoomRequest : INetworkSerializable
    {
        AppID appID;
        public AppID AppID => appID;

        string name;
        public string Name => name;

        Version version;
        public Version Version => version;

        byte capacity;
        public byte Capacity => capacity;

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref appID);
            context.Select(ref version);
            context.Select(ref name);
            context.Select(ref capacity);
            context.Select(ref attributes);
        }

        public CreateRoomRequest(AppID appID, Version version, string name, byte capacity, AttributesCollection attributes)
        {
            this.appID = appID;
            this.name = name;
            this.capacity = capacity;
            this.version = version;
            this.attributes = attributes;
        }
    }

    #region Master Server
    [Preserve]
    [Serializable]
    public struct MasterServerInfoRequest : INetworkSerializable
    {
        Version apiVersion;
        public Version ApiVersion => apiVersion;

        AppID appID;
        public AppID AppID => appID;

        Version gameVersion;
        public Version GameVersion => gameVersion;

        public void Select(INetworkSerializableResolver.Context context)
        {
            //Note to Self
            //Always Keep these in the same order to ensure backwards compatibility
            context.Select(ref apiVersion);
            //End of Note

            context.Select(ref appID);
            context.Select(ref gameVersion);
        }

        public MasterServerInfoRequest(AppID appID, Version gameVersion) : this()
        {
            apiVersion = Constants.ApiVersion;
            this.appID = appID;
            this.gameVersion = gameVersion;
        }
    }

    [Preserve]
    [Serializable]
    public struct MasterServerInfoResponse : INetworkSerializable
    {
        GameServerInfo[] servers;
        public GameServerInfo[] Servers => servers;

        public int Size => servers.Length;

        public GameServerInfo this[int index] => servers[index];

        RemoteConfig remoteConfig;
        public RemoteConfig RemoteConfig => remoteConfig;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref servers);
            context.Select(ref remoteConfig);
        }

        public MasterServerInfoResponse(GameServerInfo[] servers, RemoteConfig remoteConfig)
        {
            this.servers = servers;
            this.remoteConfig = remoteConfig;
        }
    }
    #endregion

    [Preserve]
    [Serializable]
    public struct GetLobbyInfoRequest : INetworkSerializable
    {
        AppID appID;
        public AppID AppID => appID;

        Version version;
        public Version Version => version;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref appID);
            context.Select(ref version);
        }

        public GetLobbyInfoRequest(AppID appID, Version version)
        {
            this.appID = appID;
            this.version = version;
        }
    }
    #endregion

    #region RealTime
    #region Register Client
    [Preserve]
    [Serializable]
    public struct RegisterClientRequest : INetworkSerializable
    {
        NetworkClientProfile profile;
        public NetworkClientProfile Profile => profile;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref profile);
        }

        public RegisterClientRequest(NetworkClientProfile profile)
        {
            this.profile = profile;
        }
    }

    [Preserve]
    [Serializable]
    public struct RegisterClientResponse : INetworkSerializable
    {
        NetworkClientID id;
        public NetworkClientID ID => id;

        RoomInfo room;
        public RoomInfo Room => room;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
            context.Select(ref room);
        }

        public RegisterClientResponse(NetworkClientID id, RoomInfo room)
        {
            this.id = id;
            this.room = room;
        }
    }
    #endregion

    #region Ready Client
    [Preserve]
    [Serializable]
    public struct ReadyClientRequest : INetworkSerializable
    {
        DateTime timestamp;
        public DateTime Timestamp => timestamp;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref timestamp);
        }

        ReadyClientRequest(DateTime timestamp)
        {
            this.timestamp = timestamp;
        }

        public static ReadyClientRequest Write() => new ReadyClientRequest(DateTime.UtcNow);
    }

    [Preserve]
    [Serializable]
    public struct ReadyClientResponse : INetworkSerializable
    {
        NetworkClientInfo[] clients;
        public NetworkClientInfo[] Clients => clients;

        NetworkClientID master;
        public NetworkClientID Master => master;

        List<NetworkMessage> buffer;
        public List<NetworkMessage> Buffer => buffer;

        RoomTimeResponse time;
        public RoomTimeResponse Time => time;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref clients);
            context.Select(ref buffer);
            context.Select(ref master);
            context.Select(ref time);
        }

        public ReadyClientResponse(NetworkClientInfo[] clients, NetworkClientID master, List<NetworkMessage> buffer, RoomTimeResponse time)
        {
            this.clients = clients;
            this.master = master;
            this.buffer = buffer;
            this.time = time;
        }
    }
    #endregion

    #region Spawn Entity
    [Preserve]
    [Serializable]
    public struct SpawnEntityRequest : INetworkSerializable
    {
        NetworkEntityType type;
        public NetworkEntityType Type => type;

        string resource;
        public string Resource { get { return resource; } }

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        NetworkClientID? owner;
        public NetworkClientID? Owner => owner;

        byte scene;
        public byte Scene => scene;

        ushort index;
        public ushort Index => index;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref type);

            switch (type)
            {
                case NetworkEntityType.Dynamic:
                    context.Select(ref resource);
                    context.Select(ref attributes);
                    context.Select(ref owner);
                    break;

                case NetworkEntityType.SceneObject:
                    context.Select(ref scene);
                    context.Select(ref index);
                    break;
            }
        }

        public static SpawnEntityRequest Write(string resource, AttributesCollection attributes, NetworkClientID? owner = null)
        {
            var request = new SpawnEntityRequest()
            {
                type = NetworkEntityType.Dynamic,
                resource = resource,
                attributes = attributes,
                owner = owner,
            };

            return request;
        }

        public static SpawnEntityRequest Write(byte scene, ushort index)
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

    [Preserve]
    [Serializable]
    public struct SpawnEntityCommand : INetworkSerializable
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
    [Preserve]
    [Serializable]
    public struct DestroyEntityRequest : INetworkSerializable
    {
        NetworkEntityID id;
        public NetworkEntityID ID => id;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
        }

        public DestroyEntityRequest(NetworkEntityID id)
        {
            this.id = id;
        }
    }

    [Preserve]
    [Serializable]
    public struct DestroyEntityCommand : INetworkSerializable
    {
        NetworkEntityID id;
        public NetworkEntityID ID => id;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
        }

        public DestroyEntityCommand(NetworkEntityID id)
        {
            this.id = id;
        }
    }
    #endregion

    #region Connection
    [Preserve]
    [Serializable]
    public struct ClientConnectedPayload : INetworkSerializable
    {
        NetworkClientInfo info;
        public NetworkClientInfo Info => info;

        public NetworkClientID ID => info.ID;

        public NetworkClientProfile Profile => info.Profile;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref info);
        }

        public ClientConnectedPayload(NetworkClientInfo info)
        {
            this.info = info;
        }
    }

    [Preserve]
    [Serializable]
    public struct ClientDisconnectPayload : INetworkSerializable
    {
        NetworkClientID id;
        public NetworkClientID ID => id;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
        }

        public ClientDisconnectPayload(NetworkClientID id)
        {
            this.id = id;
        }
    }
    #endregion

    [Preserve]
    [Serializable]
    public struct ChangeMasterCommand : INetworkSerializable
    {
        NetworkClientID id;
        public NetworkClientID ID => id;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
        }

        public ChangeMasterCommand(NetworkClientID id)
        {
            this.id = id;
        }
    }

    #region Time
    [Preserve]
    [Serializable]
    public struct RoomTimeRequest : INetworkSerializable
    {
        DateTime timestamp;
        public DateTime Timestamp => timestamp;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref timestamp);
        }

        RoomTimeRequest(DateTime timestamp)
        {
            this.timestamp = timestamp;
        }

        public static RoomTimeRequest Write() => new RoomTimeRequest(DateTime.UtcNow);
    }

    [Preserve]
    [Serializable]
    public struct RoomTimeResponse : INetworkSerializable
    {
        NetworkTimeSpan time;
        public NetworkTimeSpan Time => time;

        DateTime requestTimestamp;
        public DateTime RequestTimestamp => requestTimestamp;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref time);
            context.Select(ref requestTimestamp);
        }

        public RoomTimeResponse(NetworkTimeSpan time, DateTime requestTimestamp)
        {
            this.time = time;
            this.requestTimestamp = requestTimestamp;
        }
    }
    #endregion

    #region Ping
    [Preserve]
    [Serializable]
    public struct PingRequest : INetworkSerializable
    {
        DateTime timestamp;
        public DateTime Timestamp => timestamp;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref timestamp);
        }

        PingRequest(DateTime timestamp)
        {
            this.timestamp = timestamp;
        }

        public static PingRequest Write() => new PingRequest(DateTime.UtcNow);
    }

    [Preserve]
    [Serializable]
    public struct PingResponse : INetworkSerializable
    {
        DateTime timestamp;
        public DateTime Timestamp => timestamp;

        public TimeSpan GetTimeSpan() => DateTime.UtcNow - timestamp;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref timestamp);
        }

        public PingResponse(DateTime timestamp)
        {
            this.timestamp = timestamp;
        }
        public PingResponse(PingRequest request) : this(request.Timestamp) { }
    }
    #endregion
    #endregion
}