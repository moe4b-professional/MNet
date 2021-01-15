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

namespace MNet
{
    public static partial class NetworkAPI
    {
        public static class Room
        {
            internal static void Configure()
            {
                Info.Configure();
                Scenes.Configure();
                RemoteSync.Configure();
                Clients.Configure();
                Master.Configure();
                Entities.Configure();

                Client.OnConnect += Setup;
                Client.Register.OnCallback += Register;
                Client.Ready.OnCallback += Ready;
                Client.OnDisconnect += (code) => Clear();
            }

            #region Join
            public static void Join(RoomInfo info) => Join(info.ID);
            public static void Join(RoomID id) => Realtime.Connect(Server.Game.ID, id);
            #endregion

            #region Create
            public delegate void CreateDelegate(RoomInfo room, RestError error);
            public static event CreateDelegate OnCreate;
            public static void Create(string name, byte capacity, bool visibile = true, AttributesCollection attributes = null, CreateDelegate handler = null)
            {
                var payload = new CreateRoomRequest(NetworkAPI.AppID, NetworkAPI.GameVersion, name, capacity, visibile, attributes);

                Server.Game.Rest.POST<CreateRoomRequest, RoomInfo>(Constants.Server.Game.Rest.Requests.Room.Create, payload, Callback);

                void Callback(RoomInfo info, RestError error)
                {
                    handler?.Invoke(info, error);
                    OnCreate?.Invoke(info, error);
                }
            }
            #endregion

            static void Setup()
            {

            }

            static void Register(RegisterClientResponse response)
            {

            }

            public delegate void ReadyDelegate(ReadyClientResponse response);
            public static event ReadyDelegate OnReady;
            static void Ready(ReadyClientResponse response)
            {
                Info.Load(response.Room);
                Clients.AddAll(response.Clients);
                Master.Assign(response.Master);

                Realtime.ApplyBuffer(response.Buffer).Forget();

                OnReady?.Invoke(response);
            }

            public static class Info
            {
                static RoomID id;
                public static RoomID ID { get { return id; } }

                static string name;
                public static string Name { get { return name; } }

                static byte capacity;
                public static byte Capacity { get { return capacity; } }

                static byte occupancy;
                public static byte Occupancy { get { return occupancy; } }

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

                static AttributesCollection attributes;
                public static AttributesCollection Attributes => attributes;

                public static void ModifyAttribute<TValue>(ushort key, TValue value)
                {
                    var collection = new AttributesCollection();

                    collection.Set(key, value);

                    ModifyAttributes(collection);
                }

                public static void ModifyAttributes(AttributesCollection collection)
                {
                    var payload = new ChangeRoomInfoPayload() { ModifiedAttributes = collection };

                    Send(ref payload);
                }

                public static void RemoveAttributes(params ushort[] keys)
                {
                    var payload = new ChangeRoomInfoPayload() { RemovedAttributes = keys };

                    Send(ref payload);
                }

                public static void Send(ref ChangeRoomInfoPayload payload)
                {
                    if (Client.IsMaster == false)
                    {
                        Debug.LogError("Local Client cannot Change Room Info because They are Not Room Master");
                        return;
                    }

                    Change(ref payload);

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
                    attributes = info.Attributes;

                    OnLoad?.Invoke();
                }

                /// <summary>
                /// Invoked on any small Room info Change after Load
                /// </summary>
                public static event Action OnChange;
                static void Change(ref ChangeRoomInfoPayload payload)
                {
                    if (payload.ModifyVisiblity) visible = payload.Visibile;

                    if (payload.ModifyAttributes) Attributes.CopyFrom(payload.ModifiedAttributes);

                    if (payload.RemoveAttributes) Attributes.RemoveAll(payload.RemovedAttributes);

                    OnChange?.Invoke();
                }

                internal static void Clear()
                {
                    id = default;
                    name = default;
                    capacity = default;
                    occupancy = default;
                    attributes = default;
                }
            }

            public static class Master
            {
                public static NetworkClient Client { get; private set; }

                internal static void Configure()
                {
                    NetworkAPI.Client.MessageDispatcher.RegisterHandler<ChangeMasterCommand>(Change);
                }

                internal static void Clear()
                {
                    Client = default;
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

                internal static void Clear()
                {
                    Dictionary.Clear();
                }

                internal static void AddAll(IList<NetworkClientInfo> list)
                {
                    for (int i = 0; i < list.Count; i++)
                        Add(list[i]);
                }

                public delegate void AddDelegate(NetworkClient client);
                public static event AddDelegate OnAdd;
                static NetworkClient Add(NetworkClientInfo info)
                {
                    var client = Create(info);

                    Dictionary.Add(client.ID, client);

                    OnAdd?.Invoke(client);

                    return client;
                }

                static NetworkClient Create(NetworkClientInfo info)
                {
                    if (Client.Self?.ID == info.ID) return Client.Self;

                    return new NetworkClient(info);
                }

                public delegate void RemoveDelegate(NetworkClient client);
                public static event RemoveDelegate OnRemove;
                static void Remove(NetworkClient client)
                {
                    Dictionary.Remove(client.ID);

                    var entities = client.Entities;

                    for (int i = 0; i < entities.Count; i++)
                    {
                        if (entities[i].Type == EntityType.SceneObject) continue;

                        if (entities[i].Persistance.HasFlag(PersistanceFlags.PlayerDisconnection))
                        {
                            Entities.MakeOrphan(entities[i]);
                            continue;
                        }

                        Entities.Destroy(entities[i]);
                    }

                    OnRemove?.Invoke(client);
                }

                public delegate void ConnectedDelegate(NetworkClient client);
                public static event ConnectedDelegate OnConnected;
                static void Connect(ref ClientConnectedPayload payload)
                {
                    if (Dictionary.ContainsKey(payload.ID))
                    {
                        Debug.Log($"Connecting Client {payload.ID} Already Registered With Room");
                        return;
                    }

                    var client = Add(payload.Info);

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
                    Client.MessageDispatcher.RegisterHandler<ChangeEntityOwnerCommand>(ChangeOwner);
                    Client.MessageDispatcher.RegisterHandler<DestroyEntityCommand>(Destroy);
                }

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

                    var entity = Assimilate(command);

                    var owner = FindOwner(command);

                    Debug.Log($"Spawning Entity '{entity.name}' with ID: {id}, Owned By Client {owner}");

                    if (owner == null)
                        Debug.LogWarning($"Spawned Entity {entity.name} Has No Registered Owner");

                    entity.Setup(owner, type, persistance, attributes);

                    Spawn(entity, id);
                }

                internal static void SpawnLocal(NetworkEntity entity, NetworkEntityID id)
                {
                    Spawn(entity, id);
                }

                public delegate void SpawnEntityDelegate(NetworkEntity entity);
                public static event SpawnEntityDelegate OnSpawn;
                static void Spawn(NetworkEntity entity, NetworkEntityID id)
                {
                    Dictionary.Add(id, entity);

                    entity.Owner?.Entities.Add(entity);

                    if (entity.IsMasterObject) MasterObjects.Add(entity);

                    if (entity.Persistance.HasFlag(PersistanceFlags.SceneLoad)) Object.DontDestroyOnLoad(entity);

                    entity.Spawn(id);

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

                internal static NetworkEntity Instantiate(ushort resource)
                {
                    var prefab = NetworkAPI.Config.SpawnableObjects[resource];

                    if (prefab == null)
                        throw new Exception($"No Dynamic Network Spawnable Object with ID: {resource} Found to Spawn");

                    var instance = Object.Instantiate(prefab);

                    instance.name = prefab.name;

                    var entity = instance.GetComponent<NetworkEntity>();
                    if (entity == null) throw new Exception($"No {nameof(NetworkEntity)} Found on Resource {resource}");

                    return entity;
                }

                static NetworkClient FindOwner(SpawnEntityCommand command)
                {
                    switch (command.Type)
                    {
                        case EntityType.SceneObject:
                        case EntityType.Orphan:
                            return Master.Client;

                        case EntityType.Dynamic:
                            if (Clients.TryGet(command.Owner, out var owner))
                                return owner;
                            else
                                return null;

                        default:
                            throw new NotImplementedException();
                    }
                }

                static void ChangeOwner(ref ChangeEntityOwnerCommand command)
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

                    ChangeOwner(client, entity);
                }
                internal static void ChangeOwner(NetworkClient client, NetworkEntity entity)
                {
                    entity.Owner?.Entities.Remove(entity);
                    entity.SetOwner(client);
                    entity.Owner?.Entities.Add(entity);
                }

                internal static void MakeOrphan(NetworkEntity entity) //*Cocks gun with malicious intent* 
                {
                    entity.Type = EntityType.Orphan;
                    entity.SetOwner(Master.Client);

                    MasterObjects.Add(entity);
                }

                static void Destroy(ref DestroyEntityCommand command)
                {
                    if (Dictionary.TryGetValue(command.ID, out var entity) == false)
                    {
                        Debug.LogError($"Couldn't Destroy Entity {command.ID} Because It's Not Registered in Room");
                        return;
                    }

                    Destroy(entity);
                }

                public delegate void DestroyDelegate(NetworkEntity entity);
                public static event DestroyDelegate OnDestroy;
                internal static void Destroy(NetworkEntity entity)
                {
                    Debug.Log($"Destroying Entity '{entity.name}'");

                    entity.Owner?.Entities.Remove(entity);

                    Dictionary.Remove(entity.ID);

                    if (entity.IsMasterObject) MasterObjects.Remove(entity);

                    Despawn(entity);

                    OnDestroy?.Invoke(entity);

                    Object.Destroy(entity.gameObject);
                }

                internal static void DestroyAllNonPersistant()
                {
                    var entities = Dictionary.Values.ToArray();

                    for (int i = 0; i < entities.Length; i++)
                    {
                        if (entities[i].Persistance.HasFlag(PersistanceFlags.SceneLoad)) continue;

                        Destroy(entities[i]);
                    }
                }

                internal static void Despawn(NetworkEntity entity)
                {
                    entity.Despawn();

                    if (entity.Persistance.HasFlag(PersistanceFlags.SceneLoad)) Scenes.MoveToActive(entity);
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
                    Client.MessageDispatcher.RegisterHandler<RpcCommand>(InvokeRPC);
                    Client.MessageDispatcher.RegisterHandler<SyncVarCommand>(InvokeSyncVar);
                }

                internal static void Clear()
                {

                }

                static void InvokeRPC(ref RpcCommand command)
                {
                    try
                    {
                        if (Entities.TryGet(command.Entity, out var target) == false)
                        {
                            Debug.LogWarning($"No {nameof(NetworkEntity)} found with ID {command.Entity} to Invoke RPC '{command}' On");
                            if (command.Type == RpcType.Query) Client.RPR.Respond(command, RemoteResponseType.FatalFailure);
                            return;
                        }

                        target.InvokeRPC(command);
                    }
                    catch (Exception)
                    {
                        if (command.Type == RpcType.Query) Client.RPR.Respond(command, RemoteResponseType.FatalFailure);
                        throw;
                    }
                }

                static void InvokeSyncVar(ref SyncVarCommand command)
                {
                    if (Entities.TryGet(command.Entity, out var target) == false)
                    {
                        Debug.LogWarning($"No {nameof(NetworkEntity)} found with ID {command.Entity}");
                        return;
                    }

                    target.InvokeSyncVar(command);
                }
            }

            public static class Scenes
            {
                public static Scene Active => SceneManager.GetActiveScene();

                internal static void Configure()
                {
                    Client.MessageDispatcher.RegisterHandler<LoadScenesPayload>(Load);

                    LoadMethod = DefaultLoadMethod;
                }

                internal static void Clear()
                {

                }

                #region Load
                public static bool IsLoading { get; private set; } = false;

                /// <summary>
                /// Method used to load scenes, change value to control scene loading so you can add loading screen and such,
                /// no need to pause realtime or any of that in custom method, just load the scenes
                /// </summary>
                public static LoadMethodDeleagate LoadMethod { get; set; }
                public delegate UniTask LoadMethodDeleagate(byte[] scenes, LoadSceneMode mode);

                public static async UniTask DefaultLoadMethod(byte[] scenes, LoadSceneMode mode)
                {
                    for (int i = 0; i < scenes.Length; i++)
                    {
                        var scene = SceneManager.GetSceneByBuildIndex(scenes[i]);

                        if (scene.isLoaded)
                        {
                            Log.Warning($"Got Command to Load Scene at Index {scenes[i]} but That Scene is Already Loaded, " +
                                $"Loading The Same Scene Multiple Times is not Supported, Ignoring");
                            continue;
                        }

                        await SceneManager.LoadSceneAsync(scenes[i], mode);

                        if (i == 0) mode = LoadSceneMode.Additive;
                    }
                }

                static void Load(ref LoadScenesPayload payload)
                {
                    if (IsLoading) throw new Exception("Scene API Already Loading Scene Recieved new Load Scene Command While Already Loading a Previous Command");

                    var scenes = payload.Scenes;
                    var mode = ConvertLoadMode(payload.Mode);

                    Load(scenes, mode).Forget();
                }

                public static event LoadDelegate OnLoadBegin;
                static async UniTask Load(byte[] scenes, LoadSceneMode mode)
                {
                    IsLoading = true;
                    var pauseLock = Realtime.Pause.AddLock();
                    OnLoadBegin?.Invoke(scenes, mode);

                    if (mode == LoadSceneMode.Single) Entities.DestroyAllNonPersistant();

                    await LoadMethod(scenes, mode);

                    IsLoading = false;
                    Realtime.Pause.RemoveLock(pauseLock);
                    OnLoadEnd?.Invoke(scenes, mode);
                }
                public static event LoadDelegate OnLoadEnd;

                public delegate void LoadDelegate(byte[] indexes, LoadSceneMode mode);
                #endregion

                #region Request
                public static void Load(params GameScene[] scenes) => Load(LoadSceneMode.Single, scenes);
                public static void Load(LoadSceneMode mode, params GameScene[] scenes)
                {
                    var indexes = Array.ConvertAll(scenes, Convert);

                    byte Convert(GameScene element)
                    {
                        if (element.BuildIndex > byte.MaxValue)
                            throw new Exception($"Trying to Load at Build Index {element.BuildIndex}, Maximum Allowed Build Index is {byte.MaxValue}");

                        return (byte)element.BuildIndex;
                    }

                    Load(mode, indexes);
                }

                public static void Load(params byte[] indexes) => Load(LoadSceneMode.Single, indexes);
                public static void Load(LoadSceneMode mode, params byte[] indexes)
                {
                    if (Client.IsMaster == false)
                    {
                        Debug.LogWarning($"Only the Master Client Can Load Scenes, Ignoring Request");
                        return;
                    }

                    var request = new LoadScenesPayload(indexes, ConvertLoadMode(mode));

                    Load(ref request);

                    Client.Send(ref request);
                }
                #endregion

                static NetworkSceneLoadMode ConvertLoadMode(LoadSceneMode mode) => (NetworkSceneLoadMode)mode;
                static LoadSceneMode ConvertLoadMode(NetworkSceneLoadMode mode) => (LoadSceneMode)mode;

                internal static void MoveToActive(Component target) => MoveToActive(target.gameObject);
                internal static void MoveToActive(GameObject gameObject) => SceneManager.MoveGameObjectToScene(gameObject, Active);
            }

            public static void Leave() => Client.Disconnect();

            static void Clear()
            {
                Info.Clear();
                Scenes.Clear();
                RemoteSync.Clear();
                Clients.Clear();
                Master.Clear();
                Entities.Clear();
            }
        }
    }
}