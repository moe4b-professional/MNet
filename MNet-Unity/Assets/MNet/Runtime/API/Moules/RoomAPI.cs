using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using UnityEngine.Networking;

using Cysharp.Threading.Tasks;

using MB;

namespace MNet
{
    public static partial class NetworkAPI
    {
        public static class Room
        {
            internal static void Configure()
            {
                OfflineMode.Configure();
                Info.Configure();
                RemoteSync.Configure();
                Clients.Configure();
                Scenes.Configure();
                Master.Configure();
                Entities.Configure();
            }

            #region Join
            public static void Join(RoomInfo info, string password = null) => Join(info.ID, password);
            public static void Join(RoomID id, string password = null)
            {
                var server = OfflineMode.On ? default : Server.Game.ID;

                Client.Register.Password = password;

                Realtime.Connect(server, id);
            }

            public static event Client.ReadyDelegate OnJoin
            {
                add => Client.OnReady += value;
                remove => Client.OnReady -= value;
            }
            #endregion

            #region Create
            public delegate void CreateDelegate(RoomInfo room);
            public static event CreateDelegate OnCreate;
            public static async UniTask<RoomInfo> Create(string name, RoomOptions options, bool offline)
            {
                if (offline)
                {
                    var capacity = options.Capacity;
                    var attributes = options.Attributes;

                    var info = OfflineMode.Start(name, capacity, attributes);

                    OnCreate?.Invoke(info);

                    return info;
                }
                else
                {
                    var payload = new CreateRoomRequest(AppID, GameVersion, name, options);

                    var info = await Server.Game.Rest.POST<RoomInfo>(Constants.Server.Game.Rest.Requests.Room.Create, payload);

                    OnCreate?.Invoke(info);

                    return info;
                }
            }
            #endregion

            #region Leave
            public static void Leave() => Client.Disconnect();

            public static event Client.DisconnectDelegate OnLeave
            {
                add => Client.OnDisconnect += value;
                remove => Client.OnDisconnect -= value;
            }
            #endregion

            internal static void Register(ref RegisterClientResponse response)
            {
                Info.Load(response.Room);
                Clients.AddAll(response.Clients);
                Master.Assign(response.Master);

                Realtime.Buffer.Apply(response.Buffer).Forget();
            }

            public static class Info
            {
                static RoomID id;
                public static RoomID ID => id;

                static string name;
                public static string Name => name;

                static byte capacity;
                public static byte Capacity => capacity;

                static byte occupancy;
                public static byte Occupancy => occupancy;

                static bool visible;
                public static bool Visible
                {
                    get
                    {
                        return visible;
                    }
                    set
                    {
                        var payload = new ChangeRoomInfoPayload() { Visibile = value };

                        Send(ref payload);
                    }
                }

                public static class Attributes
                {
                    static AttributesCollection collection;
                    public static AttributesCollection Collection => collection;

                    internal static void Load(AttributesCollection target)
                    {
                        collection = target;
                    }

                    internal static void Change(ref ChangeRoomInfoPayload payload)
                    {
                        if (payload.ModifyAttributes) collection.CopyFrom(payload.ModifiedAttributes);

                        if (payload.RemoveAttributes) collection.RemoveAll(payload.RemovedAttributes);
                    }

                    internal static void Clear()
                    {
                        collection = default;
                    }

                    public static TValue Get<TValue>(ushort key) => collection.Get<TValue>(key);
                    public static bool TryGetValue<TValue>(ushort key, out TValue value) => collection.TryGetValue(key, out value);

                    public static bool Contains(ushort key) => collection.Contains(key);

                    public static void Set<TValue>(ushort key, TValue value)
                    {
                        var collection = new AttributesCollection();

                        collection.Set(key, value);

                        Set(collection);
                    }
                    public static void Set(AttributesCollection collection)
                    {
                        var payload = new ChangeRoomInfoPayload() { ModifiedAttributes = collection };

                        Send(ref payload);
                    }

                    public static void Remove(params ushort[] keys)
                    {
                        var payload = new ChangeRoomInfoPayload() { RemovedAttributes = keys };

                        Send(ref payload);
                    }
                }

                public static void Send(ref ChangeRoomInfoPayload payload)
                {
                    if (Client.IsMaster == false)
                    {
                        Debug.LogError("Local Client cannot Change Room Info because They are Not Room Master");
                        return;
                    }

                    Client.Send(ref payload);
                }

                /// <summary>
                /// Invoked both OnLoad & OnChange
                /// </summary>
                public static event Action OnSet
                {
                    add
                    {
                        OnLoad += value;
                        OnChange += value;
                    }
                    remove
                    {
                        OnLoad -= value;
                        OnChange -= value;
                    }
                }

                internal static void Configure()
                {
                    Client.MessageDispatcher.RegisterHandler<ChangeRoomInfoPayload>(Change);
                }

                /// <summary>
                /// Invoked when the Room's info is first read
                /// </summary>
                public static event Action OnLoad;
                internal static void Load(RoomInfo info)
                {
                    id = info.ID;
                    name = info.Name;
                    capacity = info.Capacity;
                    occupancy = info.Occupancy;
                    visible = info.Visibile;

                    Attributes.Load(info.Attributes);

                    OnLoad?.Invoke();
                }

                /// <summary>
                /// Invoked on any small Room info Change after Load
                /// </summary>
                public static event Action OnChange;
                static void Change(ref ChangeRoomInfoPayload payload)
                {
                    if (payload.ModifyVisiblity) visible = payload.Visibile;

                    Attributes.Change(ref payload);

                    OnChange?.Invoke();
                }

                internal static void Clear()
                {
                    id = default;
                    name = default;
                    capacity = default;
                    occupancy = default;

                    Attributes.Clear();
                }
            }

            public static class Master
            {
                public static NetworkClient Client { get; private set; }

                internal static void Configure()
                {
                    NetworkAPI.Client.MessageDispatcher.RegisterHandler<ChangeMasterCommand>(Change);
                }

                internal static bool Assign(NetworkClientID id)
                {
                    if (Clients.TryGet(id, out var target) == false)
                        Debug.LogError($"No Master Client With ID {id} Could be Found, Assigning Null!");

                    Client = target;
                    Debug.Log($"Assigned {Client} as Master Client");

                    Entities.UpdateMaster(Client);

                    return true;
                }

                public delegate void ChangeDelegate(NetworkClient client);
                public static event ChangeDelegate OnChange;
                static void Change(ref ChangeMasterCommand command)
                {
                    Assign(command.ID);

                    OnChange?.Invoke(Client);
                }

                internal static void Clear()
                {
                    Client = default;
                }
            }

            public static class Clients
            {
                public static Dictionary<NetworkClientID, NetworkClient> Dictionary { get; private set; }

                public static int Count => Dictionary.Count;

                public static IEnumerable<NetworkClient> List => Dictionary.Values;

                public static bool TryGet(NetworkClientID id, out NetworkClient client) => Dictionary.TryGetValue(id, out client);

                internal static void Configure()
                {
                    Dictionary = new Dictionary<NetworkClientID, NetworkClient>();

                    Client.MessageDispatcher.RegisterHandler<ClientConnectedPayload>(Connect);
                    Client.MessageDispatcher.RegisterHandler<ClientDisconnectPayload>(Disconnect);
                }

                internal static void AddAll(IList<NetworkClientInfo> list)
                {
                    for (int i = 0; i < list.Count; i++)
                        Add(list[i].ID, list[i].Profile);
                }

                public delegate void AddDelegate(NetworkClient client);
                public static event AddDelegate OnAdd;
                static NetworkClient Add(NetworkClientID id, NetworkClientProfile profile)
                {
                    var client = Assimilate(id, profile);

                    Dictionary.Add(client.ID, client);

                    OnAdd?.Invoke(client);

                    return client;
                }

                static NetworkClient Assimilate(NetworkClientID id, NetworkClientProfile profile)
                {
                    if (Client.Self?.ID == id) return Client.Self;

                    return new NetworkClient(id, profile);
                }

                public delegate void RemoveDelegate(NetworkClient client);
                public static event RemoveDelegate OnRemove;
                static void Remove(NetworkClient client)
                {
                    Dictionary.Remove(client.ID);

                    Entities.DestroyForClient(client);

                    OnRemove?.Invoke(client);
                }

                public delegate void ConnectedDelegate(NetworkClient client);
                public static event ConnectedDelegate OnConnected;
                static void Connect(ref ClientConnectedPayload payload)
                {
                    if (Dictionary.ContainsKey(payload.ID))
                    {
                        Debug.LogWarning($"Connecting Client {payload.ID} Already Registered With Room");
                        return;
                    }

                    var client = Add(payload.ID, payload.Profile);

                    OnConnected?.Invoke(client);

                    Debug.Log($"Client {client.ID} Connected to Room");
                }

                public delegate void DisconnectedDelegate(NetworkClient client);
                public static event DisconnectedDelegate OnDisconnected;
                static void Disconnect(ref ClientDisconnectPayload payload)
                {
                    Debug.Log($"Client {payload.ID} Disconnected from Room");

                    if (Dictionary.TryGetValue(payload.ID, out var client) == false)
                    {
                        Debug.Log($"Disconnecting Client {payload.ID} Not Found In Room");
                        return;
                    }

                    Remove(client);

                    OnDisconnected?.Invoke(client);
                }

                internal static void Clear()
                {
                    Dictionary.Clear();
                }
            }

            public static class Scenes
            {
                internal static void Configure()
                {
                    Load.Configure();
                    Unload.Configure();
                }

                public static class Load
                {
                    public static bool IsProcessing { get; private set; } = false;

                    internal static void Configure()
                    {
                        Client.MessageDispatcher.RegisterHandler<LoadScenePayload>(Procedure);
                    }

                    #region Procedure
                    /// <summary>
                    /// Method used to load scenes, change value to control scene loading so you can add loading screen and such,
                    /// no need to pause realtime or any of that in custom method, just load the scenes
                    /// </summary>
                    public static MethodDeleagate ProcdureMethod { get; set; } = DefaultProcedure;
                    public delegate UniTask MethodDeleagate(byte index, LoadSceneMode mode);

                    public static async UniTask DefaultProcedure(byte index, LoadSceneMode mode)
                    {
                        var scene = SceneManager.GetSceneByBuildIndex(index);

                        if (scene.isLoaded && mode == LoadSceneMode.Additive)
                        {
                            Log.Warning($"Got Command to Load Scene at Index {index} but That Scene is Already Loaded, " +
                                $"Loading The Same Scene Multiple Times is not Supported, Ignoring");
                            return;
                        }

                        await SceneManager.LoadSceneAsync(index, mode);
                    }

                    static void Procedure(ref LoadScenePayload payload)
                    {
                        if (IsProcessing) throw new Exception("Scene API Already Loading Scene Recieved new Load Scene Command While Already Loading a Previous Command");

                        var index = payload.Index;
                        var mode = ConvertLoadMode(payload.Mode);

                        Procedure(index, mode).Forget();
                    }

                    public static event EventDelegate OnBegin;
                    static async UniTask Procedure(byte index, LoadSceneMode mode)
                    {
                        IsProcessing = true;
                        var pauseLock = Realtime.Pause.AddLock();
                        OnBegin?.Invoke(index, mode);

                        if (mode == LoadSceneMode.Single) NetworkScene.UnregisterAll();

                        await ProcdureMethod(index, mode);

                        IsProcessing = false;
                        Realtime.Pause.RemoveLock(pauseLock);
                        OnEnd?.Invoke(index, mode);
                    }
                    public static event EventDelegate OnEnd;

                    public delegate void EventDelegate(byte index, LoadSceneMode mode);
                    #endregion

                    #region Request
                    public static void Request(string scene) => Request(scene, LoadSceneMode.Single);
                    public static void Request(string scene, LoadSceneMode mode)
                    {
                        if (MScenesCollection.TryFind(scene, out var asset) == false)
                            throw new Exception($"Cannot Find Scene named '{scene}' to Load");

                        Request(asset, mode);
                    }

                    public static void Request(MSceneAsset scene) => Request(scene, LoadSceneMode.Single);
                    public static void Request(MSceneAsset scene, LoadSceneMode mode)
                    {
                        var index = (byte)scene.Index;

                        Request(index, mode);
                    }

                    public static void Request(byte index) => Request(index, LoadSceneMode.Single);
                    public static void Request(byte index, LoadSceneMode mode)
                    {
                        if (Client.IsMaster == false)
                        {
                            Debug.LogWarning($"Only the Master Client may Load Scenes, Ignoring Request");
                            return;
                        }

                        if (NetworkScene.Contains(index) && mode == LoadSceneMode.Additive)
                        {
                            Debug.LogWarning($"Cannot Load Scene {index} Additively as it's Already Loaded, " +
                                $"Loading the Same Scene Multiple Times is not Supported");
                            return;
                        }

                        var request = new LoadScenePayload(index, ConvertLoadMode(mode));

                        Client.Send(ref request);
                    }
                    #endregion

                    internal static void Clear()
                    {
                        IsProcessing = false;
                    }
                }

                public static class Unload
                {
                    public static bool IsProcessing { get; private set; } = false;

                    internal static void Configure()
                    {
                        Client.MessageDispatcher.RegisterHandler<UnloadScenePayload>(Procedure);
                    }

                    #region Procedure
                    /// <summary>
                    /// Method used to load scenes, change value to control scene loading so you can add loading screen and such,
                    /// no need to pause realtime or any of that in custom method, just load the scenes
                    /// </summary>
                    public static MethodDeleagate ProcdureMethod { get; set; } = DefaultProcedure;
                    public delegate UniTask MethodDeleagate(NetworkScene scene);

                    public static async UniTask DefaultProcedure(NetworkScene scene)
                    {
                        if (scene == null)
                        {
                            Debug.LogError($"Scene to Unload is Null");
                            return;
                        }

                        var index = scene.Index;

                        await SceneManager.UnloadSceneAsync(index);
                    }

                    static void Procedure(ref UnloadScenePayload payload)
                    {
                        if (NetworkScene.TryGet(payload.Index, out var scene) == false)
                        {
                            Debug.LogError($"Recieved Request to Unload Scene {payload.Index} But that Scene is no Loaded");
                            return;
                        }

                        if (IsProcessing) throw new Exception("Scene API Already Loading Scene Recieved new Load Scene Command While Already Loading a Previous Command");

                        Procedure(scene).Forget();
                    }

                    public static event EventDelegate OnBegin;
                    static async UniTask Procedure(NetworkScene scene)
                    {
                        IsProcessing = true;
                        var pauseLock = Realtime.Pause.AddLock();
                        OnBegin?.Invoke(scene);

                        NetworkScene.Unregister(scene);

                        await ProcdureMethod(scene);

                        IsProcessing = false;
                        Realtime.Pause.RemoveLock(pauseLock);
                        OnEnd?.Invoke(scene);
                    }
                    public static event EventDelegate OnEnd;

                    public delegate void EventDelegate(NetworkScene scene);
                    #endregion

                    #region Request
                    public static void Request(string scene)
                    {
                        if (MScenesCollection.TryFind(scene, out var asset) == false)
                            throw new Exception($"Cannot Find Scene named '{scene}' to Unload");

                        Request(asset);
                    }

                    public static void Request(MSceneAsset scene)
                    {
                        var index = (byte)scene.Index;

                        Request(index);
                    }

                    public static void Request(byte index)
                    {
                        if (Client.IsMaster == false)
                        {
                            Debug.LogWarning($"Only the Master Client may Load Scenes, Ignoring Request");
                            return;
                        }

                        if (NetworkScene.Contains(index) == false)
                        {
                            Debug.LogWarning($"Cannot Unload Scene {index} Because It's not Loaded");
                            return;
                        }

                        if (NetworkScene.Count == 1)
                        {
                            Log.Warning($"Cannot Unload Scene {index} as It's the only Loaded Scene");
                            return;
                        }

                        var request = new UnloadScenePayload(index);

                        Client.Send(ref request);
                    }
                    #endregion

                    internal static void Clear()
                    {
                        IsProcessing = false;
                    }
                }

                internal static void Clear()
                {
                    Load.Clear();
                    Unload.Clear();
                }

                static NetworkSceneLoadMode ConvertLoadMode(LoadSceneMode mode) => (NetworkSceneLoadMode)mode;
                static LoadSceneMode ConvertLoadMode(NetworkSceneLoadMode mode) => (LoadSceneMode)mode;
            }

            public static class Entities
            {
                public static Dictionary<NetworkEntityID, NetworkEntity> Dictionary { get; private set; }

                public static bool TryGet(NetworkEntityID id, out NetworkEntity entity) => Dictionary.TryGetValue(id, out entity);

                public static HashSet<NetworkEntity> MasterObjects { get; private set; }

                internal static void UpdateMaster(NetworkClient client)
                {
                    foreach (var entity in MasterObjects) entity.SetOwner(client);
                }

                internal static void Configure()
                {
                    Dictionary = new Dictionary<NetworkEntityID, NetworkEntity>();
                    MasterObjects = new HashSet<NetworkEntity>();

                    Client.MessageDispatcher.RegisterHandler<SpawnEntityCommand>(SpawnRemote);
                    Client.MessageDispatcher.RegisterHandler<TransferEntityPayload>(Transfer);
                    Client.MessageDispatcher.RegisterHandler<TakeoverEntityCommand>(Takeover);
                    Client.MessageDispatcher.RegisterHandler<DestroyEntityPayload>(Destroy);
                }

                #region Spawn
                internal static void SpawnSceneObject(ushort resource, Scene scene) => SpawnSceneObject(resource, (byte)scene.buildIndex);
                internal static void SpawnSceneObject(ushort resource, byte scene)
                {
                    if (Client.IsMaster == false)
                    {
                        Debug.LogError("Only the Master Client May Spawn Scene Objects, Ignoring Request");
                        return;
                    }

                    var request = SpawnEntityRequest.Write(resource, scene);

                    Client.Send(ref request);
                }

                static void SpawnRemote(ref SpawnEntityCommand command)
                {
                    var id = command.ID;
                    var type = command.Type;
                    var persistance = command.Persistance;
                    var attributes = command.Attributes;

                    if (FindSceneFrom(ref command, out var scene) == false) return;

                    var entity = Assimilate(command);

                    var owner = FindOwner(command);

                    if (owner == null)
                        Debug.LogWarning($"Spawned Entity {entity.name} Has No Registered Owner");

                    entity.Setup(owner, type, persistance, attributes);

                    Spawn(entity, id, scene);
                }

                static bool FindSceneFrom(ref SpawnEntityCommand command, out NetworkScene scene)
                {
                    switch (command.Type)
                    {
                        case EntityType.SceneObject:
                            {
                                if (NetworkScene.TryGet(command.Scene, out scene) == false)
                                {
                                    Log.Info(NetworkScene.Dictionary);
                                    Debug.LogWarning($"Scene {command.Scene} Not Found, Cannot Spawn Scene Object");
                                    return false;
                                }

                                return true;
                            }

                        case EntityType.Dynamic:
                        case EntityType.Orphan:
                            {
                                if (command.Persistance.HasFlag(PersistanceFlags.SceneLoad))
                                {
                                    scene = null;
                                    return true;
                                }

                                if (NetworkScene.Active == null)
                                {
                                    Debug.LogWarning("Cannot Spawn Entity, No Active Scene Loaded");
                                    scene = null;
                                    return false;
                                }

                                scene = NetworkScene.Active;
                                return true;
                            }

                        default:
                            throw new NotImplementedException($"No Condition Set For {command.Type}");
                    }
                }

                internal static void SpawnLocal(NetworkEntity entity, ref SpawnEntityResponse response)
                {
                    var id = response.ID;
                    var scene = NetworkScene.Active;

                    if (scene == null)
                        throw new Exception($"No Active NetworkScene Found to Spawn Local Entity {entity}");

                    Spawn(entity, id, scene);
                }

                public delegate void SpawnEntityDelegate(NetworkEntity entity);
                public static event SpawnEntityDelegate OnSpawn;
                static void Spawn(NetworkEntity entity, NetworkEntityID id, NetworkScene scene)
                {
                    Debug.Log($"Spawning Entity '{entity.name}' with ID: {id}, Owned By Client {entity.Owner}");

                    if (entity.IsDynamic)
                    {
                        if (entity.Persistance.HasFlag(PersistanceFlags.SceneLoad))
                            Object.DontDestroyOnLoad(entity);
                        else
                            scene.AddDynamic(entity);
                    }

                    Dictionary.Add(id, entity);
                    entity.Owner?.Entities.Add(entity);
                    if (entity.IsMasterObject) MasterObjects.Add(entity);

                    entity.Spawn(id, scene);

                    OnSpawn?.Invoke(entity);
                }

                static NetworkEntity Assimilate(SpawnEntityCommand command)
                {
                    if (command.Type == EntityType.Dynamic || command.Type == EntityType.Orphan)
                    {
                        var instance = Instantiate(command.Resource);

                        return instance;
                    }

                    if (command.Type == EntityType.SceneObject)
                    {
                        var instance = NetworkScene.LocateEntity(command.Scene, command.Resource);

                        return instance;
                    }

                    throw new NotImplementedException();
                }

                static NetworkClient FindOwner(SpawnEntityCommand command)
                {
                    switch (command.Type)
                    {
                        case EntityType.SceneObject:
                        case EntityType.Orphan:
                            return Master.Client;

                        case EntityType.Dynamic:
                            Clients.TryGet(command.Owner, out var owner);
                            return owner;

                        default:
                            throw new NotImplementedException();
                    }
                }
                #endregion

                internal static NetworkEntity Instantiate(ushort resource)
                {
                    var prefab = NetworkAPI.Config.SyncedAssets[resource] as GameObject;

                    if (prefab == null)
                        throw new Exception($"No Synced Asset GameObject with ID: {resource} Found to Spawn");

                    var instance = Object.Instantiate(prefab).GetComponent<NetworkEntity>();

                    if (instance == null) throw new Exception($"No {nameof(NetworkEntity)} Found on Resource {resource}");

                    instance.name = prefab.name;

                    return instance;
                }

                #region Ownership
                static void Transfer(ref TransferEntityPayload payload)
                {
                    if (Clients.TryGet(payload.Client, out var client) == false)
                    {
                        Debug.LogWarning($"No Client {payload.Client} Found to Transfer Entity {payload.Entity} to");
                        return;
                    }

                    if (Dictionary.TryGetValue(payload.Entity, out var entity) == false)
                    {
                        Debug.LogWarning($"No Entity {payload.Entity} To be Transfered to Client {client}");
                        return;
                    }

                    ChangeOwner(entity, client);
                }

                static void Takeover(ref TakeoverEntityCommand command)
                {
                    if (Clients.TryGet(command.Client, out var client) == false)
                    {
                        Debug.LogWarning($"No Client {command.Client} Found to Takeover Entity {command.Entity}");
                        return;
                    }

                    if (Dictionary.TryGetValue(command.Entity, out var entity) == false)
                    {
                        Debug.LogWarning($"No Entity {command.Entity} To be Taken Over by Client {client}");
                        return;
                    }

                    ChangeOwner(entity, client);
                }

                internal static void ChangeOwner(NetworkEntity entity, NetworkClient client)
                {
                    entity.Owner?.Entities.Remove(entity);
                    entity.SetOwner(client);
                    entity.Owner?.Entities.Add(entity);
                }
                #endregion

                internal static void MakeOrphan(NetworkEntity entity) //*Cocks gun with malicious intent* 
                {
                    entity.Type = EntityType.Orphan;
                    MasterObjects.Add(entity);
                    entity.SetOwner(Master.Client);
                }

                #region Destroy
                static void Destroy(ref DestroyEntityPayload payload)
                {
                    if (Dictionary.TryGetValue(payload.ID, out var entity) == false)
                    {
                        Debug.LogError($"Couldn't Destroy Entity {payload.ID} Because It's Not Registered in Room");
                        return;
                    }

                    Destroy(entity);
                }

                public delegate void DestroyDelegate(NetworkEntity entity);
                public static event DestroyDelegate OnDestroy;
                internal static void Destroy(NetworkEntity entity)
                {
                    Debug.Log($"Destroying Entity '{entity.name}'");

                    Dictionary.Remove(entity.ID);
                    entity.Owner?.Entities.Remove(entity);
                    entity.NetworkScene?.RemoveDynamic(entity);
                    if (entity.IsMasterObject) MasterObjects.Remove(entity);

                    Despawn(entity);

                    OnDestroy?.Invoke(entity);

                    Object.Destroy(entity.gameObject);
                }

                internal static void DestroyForClient(NetworkClient client)
                {
                    var entities = client.Entities.ToArray();

                    for (int i = 0; i < entities.Length; i++)
                    {
                        if (entities[i].IsMasterObject) continue;

                        if (entities[i].Persistance.HasFlag(PersistanceFlags.PlayerDisconnection))
                        {
                            MakeOrphan(entities[i]);
                            continue;
                        }

                        Destroy(entities[i]);
                    }
                }

                internal static void DestroyInScene(NetworkScene scene)
                {
                    var entities = scene.Entities.ToArray();

                    for (int i = 0; i < entities.Length; i++)
                        Destroy(entities[i]);
                }
                #endregion

                internal static void Despawn(NetworkEntity entity)
                {
                    entity.Despawn();

                    if (entity.Persistance.HasFlag(PersistanceFlags.SceneLoad))
                        SceneManager.MoveGameObjectToScene(entity.gameObject, SceneManager.GetActiveScene());
                }

                internal static void Clear()
                {
                    foreach (var entity in Dictionary.Values)
                    {
                        if (entity == null)
                        {
                            Debug.LogWarning("Found null Entity when Clearing Room's Entities, Ignoring Entity");
                            continue;
                        }

                        Despawn(entity);
                    }

                    Dictionary.Clear();
                    MasterObjects.Clear();
                }
            }

            public static class RemoteSync
            {
                internal static void Configure()
                {
                    Client.MessageDispatcher.RegisterHandler<BroadcastRpcCommand>(InvokeBroadcastRPC);
                    Client.MessageDispatcher.RegisterHandler<TargetRpcCommand>(InvokeTargetRPC);
                    Client.MessageDispatcher.RegisterHandler<QueryRpcCommand>(InvokeQueryRPC);
                    Client.MessageDispatcher.RegisterHandler<BufferRpcCommand>(InvokeBufferRPC);

                    Client.MessageDispatcher.RegisterHandler<SyncVarCommand>(InvokeSyncVar);
                }

                #region RPC
                static void InvokeBroadcastRPC(ref BroadcastRpcCommand command) => InvokeRPC(ref command);
                static void InvokeTargetRPC(ref TargetRpcCommand command) => InvokeRPC(ref command);
                static void InvokeQueryRPC(ref QueryRpcCommand command) => InvokeRPC(ref command);
                static void InvokeBufferRPC(ref BufferRpcCommand command) => InvokeRPC(ref command);

                static void InvokeRPC<T>(ref T command)
                    where T : IRpcCommand
                {
                    if (Entities.TryGet(command.Entity, out var target) == false)
                    {
                        Debug.LogWarning($"No {nameof(NetworkEntity)} found with ID {command.Entity} to Invoke RPC '{command}' On");
                        if (command is QueryRpcCommand query) Client.RPR.Respond(query, RemoteResponseType.FatalFailure);
                        return;
                    }

                    if (target.InvokeRPC(ref command) == false)
                    {
                        if (command is QueryRpcCommand query) Client.RPR.Respond(query, RemoteResponseType.FatalFailure);
                    }
                }
                #endregion

                static void InvokeSyncVar(ref SyncVarCommand command)
                {
                    if (Entities.TryGet(command.Entity, out var target) == false)
                    {
                        Debug.LogWarning($"No {nameof(NetworkEntity)} found with ID {command.Entity}");
                        return;
                    }

                    target.InvokeSyncVar(command);
                }

                internal static void Clear()
                {

                }
            }

            internal static void Clear()
            {
                Info.Clear();
                RemoteSync.Clear();
                Clients.Clear();
                Scenes.Clear();
                Master.Clear();
                Entities.Clear();
            }
        }
    }
}