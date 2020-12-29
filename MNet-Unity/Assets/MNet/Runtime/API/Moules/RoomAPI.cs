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

namespace MNet
{
    public static partial class NetworkAPI
    {
        public static class Room
        {
            public static RoomInfo Info { get; private set; }

            #region Master
            public static NetworkClient Master { get; private set; }

            static bool AssignMaster(NetworkClientID id)
            {
                if (Clients.TryGetValue(id, out var target) == false)
                    Debug.LogError($"No Master Client With ID {id} Could be Found, Assigning Null!");

                Master = target;
                Debug.Log($"Assigned {Master} as Master Client");

                for (int i = 0; i < MasterObjects.Count; i++)
                {
                    if (MasterObjects[i] == null) continue;

                    MasterObjects[i].SetOwner(Master);
                }

                return true;
            }

            public delegate void ChangeMasterDelegate(NetworkClient client);
            public static event ChangeMasterDelegate OnChangeMaster;
            static void ChangeMaster(ChangeMasterCommand command)
            {
                AssignMaster(command.ID);

                OnChangeMaster?.Invoke(Master);
            }
            #endregion

            public static void Configure()
            {
                Clients = new Dictionary<NetworkClientID, NetworkClient>();
                Entities = new Dictionary<NetworkEntityID, NetworkEntity>();
                SceneObjects = new List<NetworkEntity>();
                MasterObjects = new List<NetworkEntity>();

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
            public static void Join(RoomBasicInfo info) => Join(info.ID);
            public static void Join(RoomID id) => Realtime.Connect(Server.Game.ID, id);
            #endregion

            #region Create
            public delegate void CreateDelegate(RoomBasicInfo room, RestError error);
            public static event CreateDelegate OnCreate;
            public static void Create(string name, byte capacity, AttributesCollection attributes = null)
            {
                var payload = new CreateRoomRequest(NetworkAPI.AppID, NetworkAPI.Version, name, capacity, attributes);

                Server.Game.Rest.POST<CreateRoomRequest, RoomBasicInfo>(Constants.Server.Game.Rest.Requests.Room.Create, payload, Callback);

                void Callback(RoomBasicInfo info, RestError error)
                {
                    OnCreate(info, error);
                }
            }
            #endregion

            static void Setup()
            {
                
            }

            static void Register(RegisterClientResponse response)
            {
                Info = response.Room;
            }

            public delegate void ReadyDelegate(ReadyClientResponse response);
            public static event ReadyDelegate OnReady;
            static void Ready(ReadyClientResponse response)
            {
                AddClients(response.Clients);
                AssignMaster(response.Master);

                Realtime.AddToBuffer(response.Buffer);

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
                    if (entities[i].Persistance.HasFlag(PersistanceFlags.PlayerDisconnection)) continue;

                    DestroyEntity(entities[i]);
                }

                OnRemoveClient?.Invoke(client);
            }

            public delegate void ClientConnectedDelegate(NetworkClient client);
            public static event ClientConnectedDelegate OnClientConnected;
            static void ClientConnected(ClientConnectedPayload payload)
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

            public delegate void ClientDisconnectedDelegate(NetworkClientID id, NetworkClientProfile profile);
            public static event ClientDisconnectedDelegate OnClientDisconnected;
            static void ClientDisconnected(ClientDisconnectPayload payload)
            {
                Debug.Log($"Client {payload.ID} Disconnected from Room");

                if (Clients.TryGetValue(payload.ID, out var client))
                    RemoveClient(client);
                else
                    Debug.Log($"Disconnecting Client {payload.ID} Not Found In Room");

                OnClientDisconnected?.Invoke(payload.ID, client?.Profile);
            }
            #endregion

            #region Entity
            public static Dictionary<NetworkEntityID, NetworkEntity> Entities { get; private set; }

            public static List<NetworkEntity> SceneObjects { get; private set; }

            public static List<NetworkEntity> MasterObjects { get; private set; }

            public delegate void SpawnEntityDelegate(NetworkEntity entity);
            public static event SpawnEntityDelegate OnSpawnEntity;
            static void SpawnEntity(SpawnEntityCommand command)
            {
                var entity = AssimilateEntity(command);

                Debug.Log($"Spawned '{entity.name}' with ID: {command.ID}, Owned By Client {command.Owner}");

                var owner = FindOwner(command);

                if (owner == null)
                    Debug.LogWarning($"Spawned Entity {entity.name} Has No Registered Owner");
                else
                    owner.Entities.Add(entity);

                entity.Setup(owner, command.ID, command.Attributes, command.Type, command.Persistance);

                Entities.Add(entity.ID, entity);

                if (command.Type == NetworkEntityType.SceneObject)
                {
                    SceneObjects.Add(entity);
                    MasterObjects.Add(entity);
                }

                OnSpawnEntity?.Invoke(entity);
            }

            static NetworkEntity AssimilateEntity(SpawnEntityCommand command)
            {
                if (command.Type == NetworkEntityType.Dynamic)
                {
                    var prefab = NetworkAPI.SpawnableObjects[command.Resource];

                    if (prefab == null)
                        throw new Exception($"No Dynamic Network Spawnable Object with ID: {command.Resource} Found to Spawn");

                    var instance = Object.Instantiate(prefab);

                    instance.name = $"{prefab.name} {command.Owner}";

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
                if (command.Type == NetworkEntityType.SceneObject) return Master;

                if (command.Type == NetworkEntityType.Dynamic)
                {
                    if (Clients.TryGetValue(command.Owner, out var owner))
                        return owner;
                    else
                        return null;
                }

                throw new NotImplementedException();
            }

            static void ChangeEntityOwner(ChangeEntityOwnerCommand command)
            {
                if(Clients.TryGetValue(command.Client, out var owner) == false)
                {
                    Debug.LogWarning($"No Client {command.Client} Found to Takeover Entity {command.Entity}");
                    return;
                }

                if(Entities.TryGetValue(command.Entity, out var entity) == false)
                {
                    Debug.LogWarning($"No Entity {command.Entity} To be Taken Over by Client {owner}");
                    return;
                }

                entity.Owner?.Entities.Remove(entity);
                entity.SetOwner(owner);
                entity.Owner?.Entities.Add(entity);
            }

            public delegate void DestroyEntityDelegate(NetworkEntity entity);
            public static event DestroyEntityDelegate OnDestroyEntity;
            static void DestroyEntity(DestroyEntityCommand command)
            {
                if (Entities.TryGetValue(command.ID, out var entity) == false)
                {
                    Debug.LogError($"Couldn't Destroy Entity {command.ID} Because It's Not Registered in Room");
                    return;
                }

                DestroyEntity(entity);
            }

            static void DestroyEntity(NetworkEntity entity)
            {
                Debug.Log($"Destroying '{entity.name}'");

                var owner = entity.Owner;

                Entities.Remove(entity.ID);

                if (entity.Type == NetworkEntityType.SceneObject)
                {
                    SceneObjects.Remove(entity);
                    MasterObjects.Remove(entity);
                }

                owner?.Entities.Remove(entity);

                entity.Despawn();

                OnDestroyEntity?.Invoke(entity);

                Object.Destroy(entity.gameObject);
            }
            #endregion

            #region RPC
            static void InvokeRPC(RpcCommand command)
            {
                try
                {
                    if (Entities.TryGetValue(command.Entity, out var target) == false)
                    {
                        Debug.LogWarning($"No {nameof(NetworkEntity)} found with ID {command.Entity} to Invoke RPC '{command}' On");
                        return;
                    }

                    target.InvokeRPC(command);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            #endregion

            static void InvokeSyncVar(SyncVarCommand command)
            {
                if (Entities.TryGetValue(command.Entity, out var target) == false)
                {
                    Debug.LogWarning($"No {nameof(NetworkEntity)} found with ID {command.Entity}");
                    return;
                }

                target.InvokeSyncVar(command);
            }

            public static void Leave() => Client.Disconnect();

            static void Disconnect(DisconnectCode code) => Clear();

            static void Clear()
            {
                foreach (var entity in Entities.Values)
                {
                    if (entity == null) continue;

                    entity.Despawn();
                }

                Info = default;

                Master = default;

                Entities.Clear();
                Clients.Clear();
                SceneObjects.Clear();
                MasterObjects.Clear();
            }
        }
    }
}