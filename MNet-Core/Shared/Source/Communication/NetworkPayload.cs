using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace MNet
{
    public static class NetworkPayload
    {
        public const byte MinCode = 100;

        public static Dictionary<byte, Type> Codes { get; private set; }
        public static Dictionary<Type, byte> Types { get; private set; }

        #region Get Type
        public static Type GetType(byte code)
        {
            if (TryGetType(code, out var type))
                return type;
            else
                throw new Exception($"No NetworkPayload Registerd With Code {code}");
        }

        public static bool TryGetType(byte code, out Type type) => Codes.TryGetValue(code, out type);
        #endregion

        #region Get Code
        public static byte GetCode<T>()
        {
            var type = typeof(T);

            return GetCode(type);
        }
        public static byte GetCode(Type type)
        {
            if (TryGetCode(type, out var code))
                return code;
            else
                throw new Exception($"Type {type} Not Registered as a NetworkPayload");
        }

        public static bool TryGetCode(Type type, out byte code) => Types.TryGetValue(type, out code);
        #endregion

        public static void Register<T>(byte code)
        {
            var type = typeof(T);

            Validate(code, type);

            Codes.Add(code, type);
            Types.Add(type, code);
        }

        #region Validate
        static void Validate(byte code, Type type)
        {
            ValidateDuplicate(code, type);
        }

        static void ValidateDuplicate(byte code, Type type)
        {
            ValidateTypeDuplicate(code, type);

            ValidateCodeDuplicate(code, type);
        }
        static void ValidateTypeDuplicate(byte code, Type type)
        {
            if (TryGetType(code, out var duplicate))
                throw new Exception($"NetworkPayload Type Duplicate Found, '{type}' & '{duplicate}' both registered with code '{code}'");
        }
        static void ValidateCodeDuplicate(byte code, Type type)
        {
            if (TryGetCode(type, out var duplicate))
                throw new Exception($"NetworkPayload Type Duplicate Found, Code '{code}' & '{duplicate}' Both Registered to '{type}'");
        }
        #endregion

        static void RegisterInternal()
        {
            byte index = 0;

            void Add<T>()
            {
                Register<T>(index);

                index += 1;
            }

            #region Client
            Add<RegisterClientRequest>();
            Add<RegisterClientResponse>();

            Add<ClientConnectedPayload>();
            Add<ClientDisconnectPayload>();
            #endregion

            #region Entity
            Add<SpawnEntityRequest>();
            Add<SpawnEntityResponse>();
            Add<SpawnEntityCommand>();

            Add<TransferEntityPayload>();

            Add<TakeoverEntityRequest>();
            Add<TakeoverEntityCommand>();

            Add<DestroyEntityPayload>();
            #endregion

            #region Room
            Add<ChangeMasterCommand>();

            Add<ChangeRoomInfoPayload>();
            #endregion

            #region RPC
            Add<BroadcastRpcRequest>();
            Add<BroadcastRpcCommand>();

            Add<TargetRpcRequest>();
            Add<TargetRpcCommand>();

            Add<QueryRpcRequest>();
            Add<QueryRpcCommand>();

            Add<BufferRpcRequest>();
            Add<BufferRpcCommand>();
            #endregion

            #region RPR
            Add<RprRequest>();
            Add<RprResponse>();
            Add<RprCommand>();
            #endregion

            #region SyncVar
            Add<BroadcastSyncVarRequest>();
            Add<SyncVarCommand>();
            #endregion

            #region Time
            Add<TimeRequest>();
            Add<TimeResponse>();
            #endregion

            #region Ping
            Add<PingRequest>();
            Add<PingResponse>();
            #endregion

            #region Scenes
            Add<LoadScenePayload>();
            Add<UnloadScenePayload>();
            #endregion

            #region Network Groups
            Add<JoinNetworkGroupsPayload>();
            Add<LeaveNetworkGroupsPayload>();
            #endregion
        }

        static NetworkPayload()
        {
            Codes = new Dictionary<byte, Type>();
            Types = new Dictionary<Type, byte>();

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

        bool visibile;
        public bool Visibile => visibile;

        string password;
        public string Password => password;

        MigrationPolicy migrationPolicy;
        public MigrationPolicy MigrationPolicy => migrationPolicy;

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref appID);
            context.Select(ref version);

            context.Select(ref name);
            context.Select(ref capacity);
            context.Select(ref visibile);
            context.Select(ref password);
            context.Select(ref migrationPolicy);
            context.Select(ref attributes);
        }

        public CreateRoomRequest(
            AppID appID,
            Version version,
            string name,
            byte capacity,
            bool visibile,
            string password,
            MigrationPolicy migrationPolicy,
            AttributesCollection attributes)
        {
            this.appID = appID;
            this.version = version;
            this.name = name;
            this.capacity = capacity;
            this.visibile = visibile;
            this.password = password;
            this.migrationPolicy = migrationPolicy;
            this.attributes = attributes;
        }
    }

    #region Master Server Scheme
    [Preserve]
    [Serializable]
    public struct MasterServerSchemeRequest : INetworkSerializable
    {
        Version apiVersion;
        public Version ApiVersion => apiVersion;

        AppID appID;
        public AppID AppID => appID;

        Version gameVersion;
        public Version GameVersion => gameVersion;

        public void Select(ref NetworkSerializationContext context)
        {
            //Note to Self
            //Always Keep these in the same order to ensure backwards compatibility
            context.Select(ref apiVersion);
            //End of Note

            context.Select(ref appID);
            context.Select(ref gameVersion);
        }

        public MasterServerSchemeRequest(AppID appID, Version gameVersion) : this()
        {
            apiVersion = Constants.ApiVersion;
            this.appID = appID;
            this.gameVersion = gameVersion;
        }
    }

    [Preserve]
    [Serializable]
    public struct MasterServerSchemeResponse : INetworkSerializable
    {
        AppConfig app;
        public AppConfig App => app;

        RemoteConfig remoteConfig;
        public RemoteConfig RemoteConfig => remoteConfig;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref app);
            context.Select(ref remoteConfig);
        }

        public MasterServerSchemeResponse(AppConfig app, RemoteConfig remoteConfig)
        {
            this.app = app;
            this.remoteConfig = remoteConfig;
        }
    }
    #endregion

    #region Master Server Info
    [Preserve]
    [Serializable]
    public struct MasterServerInfoRequest : INetworkSerializable
    {
        public void Select(ref NetworkSerializationContext context) { }
    }

    [Preserve]
    [Serializable]
    public struct MasterServerInfoResponse : INetworkSerializable
    {
        GameServerInfo[] servers;
        public GameServerInfo[] Servers => servers;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref servers);
        }

        public MasterServerInfoResponse(GameServerInfo[] servers)
        {
            this.servers = servers;
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

        public void Select(ref NetworkSerializationContext context)
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

        string password;
        public string Password => password;

        TimeRequest time;
        public TimeRequest Time => time;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref profile);
            context.Select(ref password);
            context.Select(ref time);
        }

        public RegisterClientRequest(NetworkClientProfile profile, string password, TimeRequest time)
        {
            this.profile = profile;
            this.password = password;
            this.time = time;
        }

        public static RegisterClientRequest Write(NetworkClientProfile profile, string password)
        {
            var time = TimeRequest.Write();

            return new RegisterClientRequest(profile, password, time);
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

        NetworkClientInfo[] clients;
        public NetworkClientInfo[] Clients => clients;

        NetworkClientID master;
        public NetworkClientID Master => master;

        NetworkMessage[] buffer;
        public NetworkMessage[] Buffer => buffer;

        TimeResponse time;
        public TimeResponse Time => time;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref id);
            context.Select(ref room);
            context.Select(ref clients);
            context.Select(ref buffer);
            context.Select(ref master);
            context.Select(ref time);
        }

        public RegisterClientResponse(NetworkClientID id, RoomInfo room, NetworkClientInfo[] clients, NetworkClientID master, NetworkMessage[] buffer, TimeResponse time)
        {
            this.id = id;
            this.room = room;
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
        EntityType type;
        public EntityType Type => type;

        ushort resource;
        public ushort Resource { get { return resource; } }

        EntitySpawnToken token;
        public EntitySpawnToken Token => token;

        PersistanceFlags persistance;
        public PersistanceFlags Persistance => persistance;

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        byte scene;
        public byte Scene => scene;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref type);

            context.Select(ref resource);

            switch (type)
            {
                case EntityType.Dynamic:
                    context.Select(ref token);
                    context.Select(ref persistance);
                    context.Select(ref attributes);
                    break;

                case EntityType.SceneObject:
                    context.Select(ref scene);
                    break;

                default:
                    Log.Error($"No Case Defined for {type} in {GetType()}");
                    break;
            }
        }

        public static SpawnEntityRequest Write(ushort resource, EntitySpawnToken token, PersistanceFlags persistance, AttributesCollection attributes)
        {
            var request = new SpawnEntityRequest()
            {
                type = EntityType.Dynamic,
                resource = resource,
                token = token,
                persistance = persistance,
                attributes = attributes,
            };

            return request;
        }

        public static SpawnEntityRequest Write(ushort resource, byte scene)
        {
            var request = new SpawnEntityRequest()
            {
                type = EntityType.SceneObject,
                scene = scene,
                resource = resource,
            };

            return request;
        }
    }

    [Preserve]
    public struct SpawnEntityResponse : INetworkSerializable
    {
        NetworkEntityID id;
        public NetworkEntityID ID => id;

        EntitySpawnToken token;
        public EntitySpawnToken Token => token;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref id);
            context.Select(ref token);
        }

        public static SpawnEntityResponse Write(NetworkEntityID id, EntitySpawnToken token)
        {
            var response = new SpawnEntityResponse()
            {
                id = id,
                token = token,
            };

            return response;
        }
    }

    [Preserve]
    [Serializable]
    public struct SpawnEntityCommand : INetworkSerializable
    {
        NetworkEntityID id;
        public NetworkEntityID ID { get { return id; } }

        ushort resource;
        public ushort Resource { get { return resource; } }

        EntityType type;
        public EntityType Type => type;

        NetworkClientID owner;
        public NetworkClientID Owner { get { return owner; } }

        PersistanceFlags persistance;
        public PersistanceFlags Persistance => persistance;

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        byte scene;
        public byte Scene => scene;

        //Yes, I know, mutable structs are "evil", I'll be careful, I swear
        public SpawnEntityCommand MakeOrphan()
        {
            type = EntityType.Orphan;

            return this;
        }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref id);

            context.Select(ref resource);

            context.Select(ref type);

            switch (type)
            {
                case EntityType.Dynamic:
                    context.Select(ref owner);
                    context.Select(ref persistance);
                    context.Select(ref attributes);
                    break;

                case EntityType.Orphan:
                    context.Select(ref persistance);
                    context.Select(ref attributes);
                    break;

                case EntityType.SceneObject:
                    context.Select(ref scene);
                    break;

                default:
                    Log.Error($"No Case Defined for {type} in {GetType()}");
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
                persistance = request.Persistance,
                resource = request.Resource,
                attributes = request.Attributes,
                scene = request.Scene
            };

            return command;
        }
    }
    #endregion

    #region Entity Ownership
    [Preserve]
    public struct TransferEntityPayload : INetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        NetworkClientID client;
        public NetworkClientID Client => client;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref entity);
            context.Select(ref client);
        }

        public TransferEntityPayload(NetworkEntityID entity, NetworkClientID client)
        {
            this.entity = entity;
            this.client = client;
        }
    }

    [Preserve]
    public struct TakeoverEntityRequest : INetworkSerializable
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref entity);
        }

        public TakeoverEntityRequest(NetworkEntityID entity)
        {
            this.entity = entity;
        }
    }

    [Preserve]
    public struct TakeoverEntityCommand : INetworkSerializable
    {
        NetworkClientID client;
        public NetworkClientID Client => client;

        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref client);
            context.Select(ref entity);
        }

        public TakeoverEntityCommand(NetworkClientID client, NetworkEntityID entity)
        {
            this.client = client;
            this.entity = entity;
        }

        public static TakeoverEntityCommand Write(NetworkClientID client, TakeoverEntityRequest request)
        {
            var command = new TakeoverEntityCommand(client, request.Entity);

            return command;
        }
    }
    #endregion

    #region Destroy Entity
    [Preserve]
    [Serializable]
    public struct DestroyEntityPayload : INetworkSerializable
    {
        NetworkEntityID id;
        public NetworkEntityID ID => id;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref id);
        }

        public DestroyEntityPayload(NetworkEntityID id)
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
        NetworkClientID id;
        public NetworkClientID ID => id;

        NetworkClientProfile profile;
        public NetworkClientProfile Profile => profile;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref id);
            context.Select(ref profile);
        }

        public ClientConnectedPayload(NetworkClientID id, NetworkClientProfile profile)
        {
            this.id = id;
            this.profile = profile;
        }
    }

    [Preserve]
    [Serializable]
    public struct ClientDisconnectPayload : INetworkSerializable
    {
        NetworkClientID id;
        public NetworkClientID ID => id;

        public void Select(ref NetworkSerializationContext context)
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

        public void Select(ref NetworkSerializationContext context)
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
    public struct TimeRequest : INetworkSerializable
    {
        DateTime timestamp;
        public DateTime Timestamp => timestamp;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref timestamp);
        }

        TimeRequest(DateTime timestamp)
        {
            this.timestamp = timestamp;
        }

        public static TimeRequest Write() => new TimeRequest(DateTime.UtcNow);
    }

    [Preserve]
    [Serializable]
    public struct TimeResponse : INetworkSerializable
    {
        NetworkTimeSpan time;
        public NetworkTimeSpan Time => time;

        DateTime timestamp;
        public DateTime Timestamp => timestamp;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref time);
            context.Select(ref timestamp);
        }

        public TimeResponse(NetworkTimeSpan time, DateTime timestamp)
        {
            this.time = time;
            this.timestamp = timestamp;
        }

        public static TimeResponse Write(NetworkTimeSpan time, TimeRequest request) => new TimeResponse(time, request.Timestamp);
    }
    #endregion

    #region Ping
    [Preserve]
    [Serializable]
    public struct PingRequest : INetworkSerializable
    {
        DateTime timestamp;
        public DateTime Timestamp => timestamp;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref timestamp);
        }

        public PingRequest(DateTime timestamp)
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

        public void Select(ref NetworkSerializationContext context)
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

    [Flags]
    public enum RoomInfoTarget : int
    {
        Visiblity = 1 << 0,
        ModifyAttributes = 1 << 1,
        RemoveAttributes = 1 << 2,
    }

    [Preserve]
    public struct ChangeRoomInfoPayload : INetworkSerializable
    {
        RoomInfoTarget targets;
        public RoomInfoTarget Targets => targets;

        void RegisterTarget(RoomInfoTarget value)
        {
            if (targets.HasFlag(value)) return;

            targets |= value;
        }

        public bool ModifyVisiblity => targets.HasFlag(RoomInfoTarget.Visiblity);
        bool visibile;
        public bool Visibile
        {
            get => visibile;
            set
            {
                RegisterTarget(RoomInfoTarget.Visiblity);

                visibile = value;
            }
        }

        public bool ModifyAttributes => targets.HasFlag(RoomInfoTarget.ModifyAttributes);
        AttributesCollection modifiedAttributes;
        public AttributesCollection ModifiedAttributes
        {
            get => modifiedAttributes;
            set
            {
                RegisterTarget(RoomInfoTarget.ModifyAttributes);
                modifiedAttributes = value;
            }
        }

        public bool RemoveAttributes => targets.HasFlag(RoomInfoTarget.RemoveAttributes);
        ushort[] removedAttributes;
        public ushort[] RemovedAttributes
        {
            get => removedAttributes;
            set
            {
                RegisterTarget(RoomInfoTarget.RemoveAttributes);

                removedAttributes = value;
            }
        }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref targets);

            if (ModifyVisiblity) context.Select(ref visibile);

            if (ModifyAttributes) context.Select(ref modifiedAttributes);

            if (RemoveAttributes) context.Select(ref removedAttributes);
        }
    }

    #region Scene
    [Preserve]
    public struct LoadScenePayload : INetworkSerializable
    {
        byte index;
        public byte Index => index;

        NetworkSceneLoadMode mode;
        public NetworkSceneLoadMode Mode => mode;

        public LoadScenePayload SetMode(NetworkSceneLoadMode value)
        {
            mode = value;
            return this;
        }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref index);
            context.Select(ref mode);
        }

        public LoadScenePayload(byte index, NetworkSceneLoadMode mode)
        {
            this.index = index;
            this.mode = mode;
        }
    }

    [Preserve]
    public struct UnloadScenePayload : INetworkSerializable
    {
        byte index;
        public byte Index => index;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref index);
        }

        public UnloadScenePayload(byte index)
        {
            this.index = index;
        }
    }
    #endregion

    #region Network Group
    [Preserve]
    public struct JoinNetworkGroupsPayload : INetworkSerializable
    {
        NetworkGroupID[] selection;
        public NetworkGroupID[] Selection => selection;

        public int Length => selection.Length;

        public NetworkGroupID this[int index] => selection[index];

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref selection);
        }

        public JoinNetworkGroupsPayload(NetworkGroupID[] selection)
        {
            this.selection = selection;
        }
    }

    [Preserve]
    public struct LeaveNetworkGroupsPayload : INetworkSerializable
    {
        NetworkGroupID[] selection;
        public NetworkGroupID[] Selection => selection;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref selection);
        }

        public LeaveNetworkGroupsPayload(NetworkGroupID[] selection)
        {
            this.selection = selection;
        }
    }
    #endregion
    #endregion
}