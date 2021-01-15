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
                    Client.RegisterMessageHandler<ChangeRoomInfoPayload>(Change);
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

            #region Master
            public static NetworkClient Master { get; private set; }

            static bool AssignMaster(NetworkClientID id)
            {
                if (Clients.TryGetValue(id, out var target) == false)
                    Debug.LogError($"No Master Client With ID {id} Could be Found, Assigning Null!");

                Master = target;
                Debug.Log($"Assigned {Master} as Master Client");

                foreach (var entity in MasterObjects) entity.SetOwner(Master);

                return true;
            }

            public delegate void ChangeMasterDelegate(NetworkClient client);
            public static event ChangeMasterDelegate OnChangeMaster;
            static void ChangeMaster(ref ChangeMasterCommand command)
            {
                AssignMaster(command.ID);

                OnChangeMaster?.Invoke(Master);
            }
            #endregion

            internal static void Configure()
            {
                Clients = new Dictionary<NetworkClientID, NetworkClient>();
                Entities = new Dictionary<NetworkEntityID, NetworkEntity>();
                MasterObjects = new HashSet<NetworkEntity>();

                Info.Configure();
                Scenes.Configure();

                Client.OnConnect += Setup;
                Client.OnRegister += Register;
                Client.OnReady += Ready;
                Client.OnDisconnect += Disconnect;

                Client.RegisterMessageHandler<RpcCommand>(InvokeRPC);
                Client.RegisterMessageHandler<SyncVarCommand>(InvokeSyncVar);

                Client.RegisterMessageHandler<SpawnEntityCommand>(SpawnEntity);
                Client.RegisterMessageHandler<DestroyEntityCommand>(DestroyEntity);

                Client.RegisterMessageHandler<ClientConnectedPayload>(ClientConnected);
                Client.RegisterMessageHandler<ClientDisconnectPayload>(ClientDisconnected);

                Client.RegisterMessageHandler<ChangeMasterCommand>(ChangeMaster);

                Client.RegisterMessageHandler<ChangeEntityOwnerCommand>(ChangeEntityOwner);
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

                AddClients(response.Clients);
                AssignMaster(response.Master);

                Realtime.ApplyBuffer(response.Buffer).Forget();

                OnReady?.Invoke(response);
            }

            #region Clients
            public static Dictionary<NetworkClientID, NetworkClient> Clients { get; private set; }

            static void AddClients(IList<NetworkClientInfo> list)
            {
                for (int i = 0; i < list.Count; i++)
                    AddClient(list[i]);
            }

            public delegate void AddClientDelegate(NetworkClient client);
            public static event AddClientDelegate OnAddClient;
            static NetworkClient AddClient(NetworkClientInfo info)
            {
                var client = CreateClient(info);

                Clients.Add(client.ID, client);

                OnAddClient?.Invoke(client);

                return client;
            }

            static NetworkClient CreateClient(NetworkClientInfo info)
            {
                if (Client.Self?.ID == info.ID) return Client.Self;

                return new NetworkClient(info);
            }

            public delegate void RemoveClientDelegate(NetworkClient client);
            public static event RemoveClientDelegate OnRemoveClient;
            static void RemoveClient(NetworkClient client)
            {
                Clients.Remove(client.ID);

                var entities = client.Entities;

                for (int i = 0; i < entities.Count; i++)
                {
                    if (entities[i].Type == NetworkEntityType.SceneObject) continue;

                    if (entities[i].Persistance.HasFlag(PersistanceFlags.PlayerDisconnection))
                    {
                        MakeEntityOrphan(entities[i]);
                        continue;
                    }

                    DestroyEntity(entities[i]);
                }

                OnRemoveClient?.Invoke(client);
            }

            public delegate void ClientConnectedDelegate(NetworkClient client);
            public static event ClientConnectedDelegate OnClientConnected;
            static void ClientConnected(ref ClientConnectedPayload payload)
            {
                if (Clients.ContainsKey(payload.ID))
                {
                    Debug.Log($"Connecting Client {payload.ID} Already Registered With Room");
                    return;
                }

                var client = AddClient(payload.Info);

                OnClientConnected?.Invoke(client);

                Debug.Log($"Client {client.ID} Connected to Room");
            }

            public delegate void ClientDisconnectedDelegate(NetworkClient client);
            public static event ClientDisconnectedDelegate OnClientDisconnected;
            static void ClientDisconnected(ref ClientDisconnectPayload payload)
            {
                Debug.Log($"Client {payload.ID} Disconnected from Room");

                if (Clients.TryGetValue(payload.ID, out var client) == false)
                {
                    Debug.Log($"Disconnecting Client {payload.ID} Not Found In Room");
                    return;
                }

                RemoveClient(client);

                OnClientDisconnected?.Invoke(client);
            }
            #endregion

            #region Entity
            public static Dictionary<NetworkEntityID, NetworkEntity> Entities { get; private set; }

            public static HashSet<NetworkEntity> MasterObjects { get; private set; }

            public delegate void SpawnEntityDelegate(NetworkEntity entity);
            public static event SpawnEntityDelegate OnSpawnEntity;
            static void SpawnEntity(ref SpawnEntityCommand command)
            {
                var id = command.ID;
                var type = command.Type;
                var persistance = command.Persistance;
                var attributes = command.Attributes;

                var entity = AssimilateEntity(command);

                var owner = FindOwner(command);

                Debug.Log($"Spawned Entity '{entity.name}' with ID: {id}, Owned By Client {owner}");

                if (owner == null)
                    Debug.LogWarning($"Spawned Entity {entity.name} Has No Registered Owner");

                owner?.Entities.Add(entity);

                Entities.Add(id, entity);

                if (NetworkEntity.CheckIfMasterObject(type)) MasterObjects.Add(entity);

                if (persistance.HasFlag(PersistanceFlags.SceneLoad)) Object.DontDestroyOnLoad(entity);

                //Scene Objects are Setup on NetworkScene Awake
                if (type != NetworkEntityType.SceneObject) entity.Setup();

                entity.Load(owner, id, attributes, type, persistance);

                OnSpawnEntity?.Invoke(entity);
            }

            static NetworkEntity AssimilateEntity(SpawnEntityCommand command)
            {
                if (command.Type == NetworkEntityType.Dynamic || command.Type == NetworkEntityType.Orphan)
                {
                    var prefab = NetworkAPI.Config.SpawnableObjects[command.Resource];

                    if (prefab == null)
                        throw new Exception($"No Dynamic Network Spawnable Object with ID: {command.Resource} Found to Spawn");

                    var instance = Object.Instantiate(prefab);

                    instance.name = $"{prefab.name} {command.ID}";

                    var entity = instance.GetComponent<NetworkEntity>();
                    if (entity == null) throw new Exception($"No {nameof(NetworkEntity)} Found on Resource {command.Resource}");

                    return entity;
                }

                if (command.Type == NetworkEntityType.SceneObject)
                {
                    var scene = NetworkScene.Find(command.Scene);

                    if (scene == null) throw new Exception($"Couldn't Find Scene {command.Scene} to Spawn Scene Object {command.Resource}");

                    if (scene.Find(command.Resource, out var entity) == false)
                        throw new Exception($"Couldn't Find NetworkBehaviour {command.Resource} In Scene {command.Scene}");

                    return entity;
                }

                throw new NotImplementedException();
            }

            static NetworkClient FindOwner(SpawnEntityCommand command)
            {
                switch (command.Type)
                {
                    case NetworkEntityType.SceneObject:
                        return Master;

                    case NetworkEntityType.Dynamic:
                        if (Clients.TryGetValue(command.Owner, out var owner))
                            return owner;
                        else
                            return null;

                    case NetworkEntityType.Orphan:
                        return Master;

                    default:
                        throw new NotImplementedException();
                }
            }

            static void ChangeEntityOwner(ref ChangeEntityOwnerCommand command)
            {
                if (Clients.TryGetValue(command.Client, out var client) == false)
                {
                    Debug.LogWarning($"No Client {command.Client} Found to Takeover Entity {command.Entity}");
                    return;
                }

                if (Entities.TryGetValue(command.Entity, out var entity) == false)
                {
                    Debug.LogWarning($"No Entity {command.Entity} To be Taken Over by Client {client}");
                    return;
                }

                ChangeEntityOwner(client, entity);
            }
            internal static void ChangeEntityOwner(NetworkClient client, NetworkEntity entity)
            {
                entity.Owner?.Entities.Remove(entity);
                entity.SetOwner(client);
                entity.Owner?.Entities.Add(entity);
            }

            static void MakeEntityOrphan(NetworkEntity entity)
            {
                entity.Type = NetworkEntityType.Orphan;
                entity.SetOwner(Master);

                MasterObjects.Add(entity);
            }

            static void DestroyEntity(ref DestroyEntityCommand command)
            {
                if (Entities.TryGetValue(command.ID, out var entity) == false)
                {
                    Debug.LogError($"Couldn't Destroy Entity {command.ID} Because It's Not Registered in Room");
                    return;
                }

                DestroyEntity(entity);
            }

            public delegate void DestroyEntityDelegate(NetworkEntity entity);
            public static event DestroyEntityDelegate OnDestroyEntity;
            internal static void DestroyEntity(NetworkEntity entity)
            {
                Debug.Log($"Destroying Entity '{entity.name}'");

                entity.Owner?.Entities.Remove(entity);

                Entities.Remove(entity.ID);

                if (entity.IsMasterObject) MasterObjects.Remove(entity);

                DespawnEntity(entity);

                OnDestroyEntity?.Invoke(entity);

                Object.Destroy(entity.gameObject);
            }

            internal static void DespawnEntity(NetworkEntity entity)
            {
                entity.Despawn();

                if (entity.Persistance.HasFlag(PersistanceFlags.SceneLoad)) Scenes.MoveToActive(entity);
            }
            #endregion

            #region Remote Sync
            static void InvokeRPC(ref RpcCommand command)
            {
                try
                {
                    if (Entities.TryGetValue(command.Entity, out var target) == false)
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
                if (Entities.TryGetValue(command.Entity, out var target) == false)
                {
                    Debug.LogWarning($"No {nameof(NetworkEntity)} found with ID {command.Entity}");
                    return;
                }

                target.InvokeSyncVar(command);
            }
            #endregion

            public static class Scenes
            {
                public static Scene Active => SceneManager.GetActiveScene();

                internal static void Configure()
                {
                    Client.RegisterMessageHandler<LoadScenesCommand>(Load);

                    LoadMethod = DefaultLoadMethod;
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

                static void Load(ref LoadScenesCommand command)
                {
                    if (IsLoading) throw new Exception("Scene API Already Loading Scene Recieved new Load Scene Command While Already Loading a Previous Command");

                    var scenes = command.Scenes;
                    var mode = ConvertLoadMode(command.Mode);

                    Load(scenes, mode).Forget();
                }

                public static event LoadDelegate OnLoadBegin;
                static async UniTask Load(byte[] scenes, LoadSceneMode mode)
                {
                    IsLoading = true;
                    var pauseLock = Realtime.Pause.AddLock();
                    OnLoadBegin?.Invoke(scenes, mode);

                    if (mode == LoadSceneMode.Single) DestoryNonPersistantEntities();

                    await LoadMethod(scenes, mode);

                    IsLoading = false;
                    Realtime.Pause.RemoveLock(pauseLock);
                    OnLoadEnd?.Invoke(scenes, mode);
                }
                public static event LoadDelegate OnLoadEnd;

                public delegate void LoadDelegate(byte[] indexes, LoadSceneMode mode);
                #endregion

                static void DestoryNonPersistantEntities()
                {
                    var entities = Room.Entities.Values.ToArray();

                    for (int i = 0; i < entities.Length; i++)
                    {
                        if (entities[i].Persistance.HasFlag(PersistanceFlags.SceneLoad)) continue;

                        Room.DestroyEntity(entities[i]);
                    }
                }

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

                    var request = new LoadScenesRequest(indexes, ConvertLoadMode(mode));

                    Client.Send(ref request);
                }
                #endregion

                static NetworkSceneLoadMode ConvertLoadMode(LoadSceneMode mode) => (NetworkSceneLoadMode)mode;
                static LoadSceneMode ConvertLoadMode(NetworkSceneLoadMode mode) => (LoadSceneMode)mode;

                internal static void MoveToActive(Component target) => MoveToActive(target.gameObject);
                internal static void MoveToActive(GameObject gameObject) => SceneManager.MoveGameObjectToScene(gameObject, Active);
            }

            public static void Leave() => Client.Disconnect();

            static void Disconnect(DisconnectCode code) => Clear();

            static void Clear()
            {
                foreach (var entity in Entities.Values)
                {
                    if (entity == null)
                    {
                        Debug.LogWarning("Found null Entity when Clearing Room's Entities, Ignoring Entity");
                        continue;
                    }

                    DespawnEntity(entity);
                }

                Info.Clear();

                Master = default;

                Entities.Clear();
                Clients.Clear();
                MasterObjects.Clear();
            }
        }
    }
}