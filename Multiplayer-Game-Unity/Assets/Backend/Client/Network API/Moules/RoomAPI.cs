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

namespace Backend
{
	public static partial class NetworkAPI
	{
        public static class Room
        {
            public static NetworkClient Master { get; private set; }

            static bool AssignMaster(NetworkClientID id)
            {
                if (Clients.TryGetValue(id, out var target) == false)
                {
                    Debug.LogError($"No Master Client With ID {id} Could be Found, Assigning Null!");
                    Master = null;
                    return false;
                }

                Master = target;
                Debug.Log($"Assigned {Master} as Master Client");

                for (int i = 0; i < SceneObjects.Count; i++)
                    SceneObjects[i].SetOwner(Master);

                return true;
            }

            public static Dictionary<NetworkClientID, NetworkClient> Clients { get; private set; }
            public static Dictionary<NetworkEntityID, NetworkEntity> Entities { get; private set; }

            public static List<NetworkEntity> SceneObjects { get; private set; }

            public static void Configure()
            {

            }

            public static void Join(RoomBasicInfo info) => Join(info.ID);
            public static void Join(RoomID id) => RealtimeAPI.Connect(id.Value);

            #region Self Callbacks
            static void SelfConnectCallback() => Setup();

            static void SelfReadyCallback(ReadyClientResponse response) => Ready(response);

            static void SelfDisconnectCallback() => Clear();
            #endregion

            #region Internal
            static void Setup()
            {
                Clients = new Dictionary<NetworkClientID, NetworkClient>();

                Entities = new Dictionary<NetworkEntityID, NetworkEntity>();

                SceneObjects = new List<NetworkEntity>();
            }

            public delegate void ReadyDelegate(ReadyClientResponse response);
            public static event ReadyDelegate OnReady;
            static void Ready(ReadyClientResponse response)
            {
                AddClients(response.Clients);
                AssignMaster(response.Master);
                ApplyMessageBuffer(response.Buffer);

                OnReady?.Invoke(response);
            }

            static void ApplyMessageBuffer(IList<NetworkMessage> list)
            {
                for (int i = 0; i < list.Count; i++)
                    MessageCallback(list[i]);
            }

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
                if (Client.Instance?.ID == info.ID) return Client.Instance;

                return new NetworkClient(info);
            }

            public delegate void RemoveClientDelegate(NetworkClient client);
            public static event RemoveClientDelegate OnRemoveClient;
            static void RemoveClient(NetworkClient client)
            {
                Clients.Remove(client.ID);

                OnRemoveClient?.Invoke(client);
            }
            #endregion

            #region Messages
            static void MessageCallback(NetworkMessage message)
            {
                if (Client.IsReady)
                {
                    if (message.Is<RpcCommand>())
                    {
                        var command = message.Read<RpcCommand>();

                        InvokeRPC(command);
                    }
                    else if (message.Is<SpawnEntityCommand>())
                    {
                        var command = message.Read<SpawnEntityCommand>();

                        SpawnEntity(command);
                    }
                    else if (message.Is<DestroyEntityCommand>())
                    {
                        var command = message.Read<DestroyEntityCommand>();

                        DestoryEntity(command);
                    }
                    else if (message.Is<RprCommand>())
                    {
                        var payload = message.Read<RprCommand>();

                        InvokeRPR(payload);
                    }
                    else if (message.Is<SyncVarCommand>())
                    {
                        var command = message.Read<SyncVarCommand>();

                        InvokeSyncVar(command);
                    }
                    else if (message.Is<ClientConnectedPayload>())
                    {
                        var payload = message.Read<ClientConnectedPayload>();

                        ClientConnected(payload);
                    }
                    else if (message.Is<ClientDisconnectPayload>())
                    {
                        var payload = message.Read<ClientDisconnectPayload>();

                        ClientDisconnected(payload);
                    }
                    else if (message.Is<ChangeMasterCommand>())
                    {
                        var command = message.Read<ChangeMasterCommand>();

                        ChangeMaster(command);
                    }
                }
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

            #region RPC
            static void InvokeRPC(RpcCommand command)
            {
                if (Entities.TryGetValue(command.Entity, out var target) == false)
                {
                    Debug.LogWarning($"No {nameof(NetworkEntity)} found with ID {command.Entity}");

                    ResolveRPC(command, RprResult.InvalidEntity);

                    return;
                }

                target.InvokeRPC(command);
            }

            public static void ResolveRPC(RpcCommand command, RprResult result)
            {
                if (command.Type == RpcType.Return) ResolveRPR(command, result);
            }
            #endregion

            #region RPR
            static void InvokeRPR(RprCommand payload)
            {
                if (Entities.TryGetValue(payload.Entity, out var target) == false)
                {
                    Debug.LogWarning($"No {nameof(NetworkEntity)} found with ID {payload.Entity}");
                    return;
                }

                target.InvokeRPR(payload);
            }

            public static void ResolveRPR(RpcCommand command, RprResult result)
            {
                if (command.Type != RpcType.Return)
                {
                    Debug.LogWarning($"Trying to Resolve RPR for Non Return Type RPC Command {command.Method}, Ignoring");
                    return;
                }

                var request = RprRequest.Write(command.Entity, command.Sender, command.Callback, result);

                Client.Send(request);
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

            public delegate void SpawnEntityDelegate(NetworkEntity entity);
            public static event SpawnEntityDelegate OnSpawnEntity;
            static void SpawnEntity(SpawnEntityCommand command)
            {
                var entity = CreateEntity(command);

                Debug.Log($"Spawned '{entity.name}' with ID: {command.ID}, Owned By Client {command.Owner}");

                var owner = FindEntityOwner(command);

                if (owner == null)
                    Debug.LogWarning($"Spawned Entity {entity.name} Has No Registered Owner");
                else
                    owner.Entities.Add(entity);

                entity.Spawn(owner, command.ID, command.Attributes, command.Type);
                Entities.Add(entity.ID, entity);

                if (command.Type == NetworkEntityType.SceneObject) SceneObjects.Add(entity);

                OnSpawnEntity?.Invoke(entity);
            }

            static NetworkClient FindEntityOwner(SpawnEntityCommand command)
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

            static NetworkEntity CreateEntity(SpawnEntityCommand command)
            {
                if (command.Type == NetworkEntityType.Dynamic)
                {
                    var prefab = Resources.Load<GameObject>(command.Resource);
                    if (prefab == null) throw new Exception($"No Resource {command.Resource} Found to Spawn");

                    var instance = Object.Instantiate(prefab);

                    instance.name = $"{command.Resource} {command.ID}";

                    var entity = instance.GetComponent<NetworkEntity>();
                    if (entity == null) throw new Exception($"No {nameof(NetworkEntity)} Found on Resource {command.Resource}");

                    return entity;
                }

                if (command.Type == NetworkEntityType.SceneObject)
                {
                    var scene = NetworkScene.Get(command.Scene);

                    if (scene == null) throw new Exception($"Couldn't Find Scene {command.Scene} to Spawn Scene Object {command.Index}");

                    if (scene.Find(command.Index, out var entity) == false)
                        throw new Exception($"Couldn't Find NetworkBehaviour {command.Index} In Scene {command.Scene}");

                    return entity;
                }

                throw new NotImplementedException();
            }

            public delegate void DestoryEntityDelegate(NetworkEntity entity);
            public static event DestoryEntityDelegate OnDestoryEntity;
            static void DestoryEntity(DestroyEntityCommand command)
            {
                if (Entities.TryGetValue(command.ID, out var entity) == false)
                {
                    Debug.LogError($"Couldn't Destroy Entity {command.ID} Because It's Not Registered in Room");
                    return;
                }

                Debug.Log("Destroying " + entity.name);

                var owner = entity.Owner;

                Entities.Remove(entity.ID);
                owner?.Entities.Remove(entity);

                entity.Despawn();

                OnDestoryEntity?.Invoke(entity);
            }

            public delegate void ChangeMasterDelegate(NetworkClient client);
            public static event ChangeMasterDelegate OnChangeMaster;
            static void ChangeMaster(ChangeMasterCommand command)
            {
                AssignMaster(command.ID);

                OnChangeMaster?.Invoke(Master);
            }

            public delegate void ClientDisconnectedDelegate(NetworkClientID id, NetworkClientProfile profile);
            public static event ClientDisconnectedDelegate OnClientDisconnected;
            static void ClientDisconnected(ClientDisconnectPayload payload)
            {
                Debug.Log($"Client {payload.ID} Disconnected to Room");

                if (Clients.TryGetValue(payload.ID, out var client))
                    RemoveClient(client);
                else
                    Debug.Log($"Disconnecting Client {payload.ID} Not Found In Room");

                OnClientDisconnected?.Invoke(payload.ID, client?.Profile);
            }
            #endregion

            static void Clear()
            {
                foreach (var entity in Entities.Values)
                {
                    if (entity == null) continue;

                    entity.Despawn();
                }

                Master = null;

                Entities.Clear();
                Clients.Clear();
                SceneObjects.Clear();
            }

            public static void Leave() => Client.Disconnect();

            static Room()
            {
                Client.OnConnect += SelfConnectCallback;
                Client.OnReady += SelfReadyCallback;
                Client.OnDisconnect += SelfDisconnectCallback;

                Client.OnMessage += MessageCallback;
            }
        }
    }
}