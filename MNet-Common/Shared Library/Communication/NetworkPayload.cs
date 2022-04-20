using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
            Register(code, type);
        }
        public static void Register(byte code, Type type)
        {
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

        static NetworkPayload()
        {
            Codes = new Dictionary<byte, Type>();
            Types = new Dictionary<Type, byte>();

            Register(0, typeof(void));

            byte index = 1;
            void Add<T>(ref byte index)
            {
                Register<T>(index);

                index += 1;
            }

            #region Client
            Add<RegisterClientRequest>(ref index);
            Add<RegisterClientResponse>(ref index);

            Add<ClientConnectedPayload>(ref index);
            Add<ClientDisconnectPayload>(ref index);
            #endregion

            #region Entity
            Add<SpawnEntityRequest>(ref index);
            Add<SpawnEntityResponse>(ref index);
            Add<SpawnEntityCommand>(ref index);

            Add<TransferEntityPayload>(ref index);

            Add<TakeoverEntityRequest>(ref index);
            Add<TakeoverEntityCommand>(ref index);

            Add<DestroyEntityPayload>(ref index);
            #endregion

            #region Room
            Add<ChangeMasterCommand>(ref index);

            Add<ChangeRoomInfoPayload>(ref index);
            #endregion

            #region RPC
            Add<BroadcastRpcRequest>(ref index);
            Add<BroadcastRpcCommand>(ref index);

            Add<TargetRpcRequest>(ref index);
            Add<TargetRpcCommand>(ref index);

            Add<QueryRpcRequest>(ref index);
            Add<QueryRpcCommand>(ref index);

            Add<BufferRpcRequest>(ref index);
            Add<BufferRpcCommand>(ref index);
            #endregion

            #region RPR
            Add<RprRequest>(ref index);
            Add<RprResponse>(ref index);
            Add<RprCommand>(ref index);
            #endregion

            #region SyncVar
            Add<BroadcastSyncVarRequest>(ref index);
            Add<SyncVarCommand>(ref index);
            #endregion

            #region Time
            Add<TimeRequest>(ref index);
            Add<TimeResponse>(ref index);
            #endregion

            #region Ping
            Add<PingRequest>(ref index);
            Add<PingResponse>(ref index);
            #endregion

            #region Scenes
            Add<LoadScenePayload>(ref index);
            Add<UnloadScenePayload>(ref index);
            #endregion

            #region Network Groups
            Add<JoinNetworkGroupsPayload>(ref index);
            Add<LeaveNetworkGroupsPayload>(ref index);
            #endregion

            Add<ServerLogPayload>(ref index);

            Add<SystemMessagePayload>(ref index);
        }
    }

    #region Register Client
    [Preserve]
    [Serializable]
    public struct RegisterClientRequest : INetworkSerializable
    {
        NetworkClientProfile profile;
        public NetworkClientProfile Profile => profile;

        FixedString16 password;
        public FixedString16 Password => password;

        TimeRequest time;
        public TimeRequest Time => time;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref profile);
            context.Select(ref password);
            context.Select(ref time);
        }

        public RegisterClientRequest(NetworkClientProfile profile, FixedString16 password, TimeRequest time)
        {
            this.profile = profile;
            this.password = password;
            this.time = time;
        }

        public static RegisterClientRequest Write(NetworkClientProfile profile, FixedString16 password)
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

        ByteChunk buffer;
        public ByteChunk Buffer => buffer;

        TimeResponse time;
        public TimeResponse Time => time;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref buffer);
            context.Select(ref id);
            context.Select(ref room);
            context.Select(ref clients);
            context.Select(ref master);
            context.Select(ref time);
        }

        public RegisterClientResponse(NetworkClientID id, RoomInfo room, NetworkClientInfo[] clients, NetworkClientID master, ByteChunk buffer, TimeResponse time)
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
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct SpawnEntityResponse
    {
        NetworkEntityID id;
        public NetworkEntityID ID => id;

        EntitySpawnToken token;
        public EntitySpawnToken Token => token;

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
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct TransferEntityPayload
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        NetworkClientID client;
        public NetworkClientID Client => client;

        public TransferEntityPayload(NetworkEntityID entity, NetworkClientID client)
        {
            this.entity = entity;
            this.client = client;
        }
    }

    [Preserve]
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct TakeoverEntityRequest
    {
        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        public TakeoverEntityRequest(NetworkEntityID entity)
        {
            this.entity = entity;
        }
    }

    [Preserve]
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct TakeoverEntityCommand
    {
        NetworkClientID client;
        public NetworkClientID Client => client;

        NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

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
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct DestroyEntityPayload
    {
        NetworkEntityID id;
        public NetworkEntityID ID => id;

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
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct ClientDisconnectPayload
    {
        NetworkClientID id;
        public NetworkClientID ID => id;

        public ClientDisconnectPayload(NetworkClientID id)
        {
            this.id = id;
        }
    }
    #endregion

    [Preserve]
    [Serializable]
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct ChangeMasterCommand
    {
        NetworkClientID id;
        public NetworkClientID ID => id;

        public ChangeMasterCommand(NetworkClientID id)
        {
            this.id = id;
        }
    }

    #region Time
    [Preserve]
    [Serializable]
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct TimeRequest
    {
        DateTime timestamp;
        public DateTime Timestamp => timestamp;

        TimeRequest(DateTime timestamp)
        {
            this.timestamp = timestamp;
        }

        public static TimeRequest Write() => new TimeRequest(DateTime.UtcNow);
    }

    [Preserve]
    [Serializable]
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct TimeResponse
    {
        NetworkTimeSpan time;
        public NetworkTimeSpan Time => time;

        DateTime timestamp;
        public DateTime Timestamp => timestamp;

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
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PingRequest
    {
        DateTime timestamp;
        public DateTime Timestamp => timestamp;

        public PingRequest(DateTime timestamp)
        {
            this.timestamp = timestamp;
        }

        public static PingRequest Write() => new PingRequest(DateTime.UtcNow);
    }

    [Preserve]
    [Serializable]
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PingResponse
    {
        DateTime timestamp;
        public DateTime Timestamp => timestamp;

        public TimeSpan GetTimeSpan() => DateTime.UtcNow - timestamp;

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
            targets |= value;
        }

        public bool ModifyVisiblity => targets.HasFlagFast(RoomInfoTarget.Visiblity);
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

        public bool ModifyAttributes => targets.HasFlagFast(RoomInfoTarget.ModifyAttributes);
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

        public bool RemoveAttributes => targets.HasFlagFast(RoomInfoTarget.RemoveAttributes);
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
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct LoadScenePayload
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

    [Preserve]
    public struct ServerLogPayload : INetworkSerializable
    {
        Log.Level level;
        public Log.Level Level => level;

        string text;
        public string Text => text;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref level);
            context.Select(ref text);
        }

        public ServerLogPayload(string text, Log.Level level)
        {
            this.text = text;
            this.level = level;
        }
    }

    [Preserve]
    public struct SystemMessagePayload : INetworkSerializable
    {
        string text;
        public string Text => text;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref text);
        }

        public SystemMessagePayload(string text)
        {
            this.text = text;
        }
    }
}