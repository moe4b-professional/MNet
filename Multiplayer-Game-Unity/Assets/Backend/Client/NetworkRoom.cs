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

using Game.Shared;

using UnityEngine.Networking;
using UnityEngine.PlayerLoop;
using UnityEngine.LowLevel;

using WebSocketSharp;
using WebSocketSharp.Net;

using System.Collections.Concurrent;

namespace Game
{
    public class NetworkRoom
    {
        public static Dictionary<NetworkClientID, NetworkClient> Clients { get; protected set; }

        public static Dictionary<NetworkEntityID, NetworkEntity> Entities { get; private set; }

        void ConnectCallback()
        {
            Debug.Log("Connected to Room");

            NetworkAPI.Client.Register();
        }

        #region Messages
        void MessageCallback(NetworkMessage message)
        {
            if (message.Is<RegisterClientResponse>())
            {
                var response = message.Read<RegisterClientResponse>();

                Register(response);
            }
            else if (message.Is<ReadyClientResponse>())
            {
                var response = message.Read<ReadyClientResponse>();

                Ready(response);
            }
            else if (message.Is<SpawnEntityCommand>())
            {
                var command = message.Read<SpawnEntityCommand>();

                Spawn(command);
            }
            else if (message.Is<RpcCommand>())
            {
                var command = message.Read<RpcCommand>();

                InvokeRpc(command);
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
        }

        void ClientConnected(ClientConnectedPayload payload)
        {
            var client = new NetworkClient(payload.ID, payload.Profile);

            Clients.Add(client.ID, client);
        }

        void Register(RegisterClientResponse response)
        {
            NetworkAPI.Client.Set(response.ID);

            Debug.Log("Register");

            NetworkAPI.Client.Ready();
        }

        void Ready(ReadyClientResponse response)
        {
            for (int i = 0; i < response.Buffer.Count; i++)
                MessageCallback(response.Buffer[i]);

            NetworkAPI.Room.Spawn("Player");
        }

        void InvokeRpc(RpcCommand command)
        {
            if (Entities.TryGetValue(command.Entity, out var target))
                target.InvokeRpc(command);
            else
                Debug.LogWarning($"No {nameof(NetworkEntity)} found with ID {command.Entity}");
        }

        void Spawn(SpawnEntityCommand command)
        {
            Debug.Log($"Spawned {command.Resource} with ID: {command.Entity}");

            var prefab = Resources.Load<GameObject>(command.Resource);
            if (prefab == null)
            {
                Debug.LogError($"No Resource {command.Resource} Found to Spawn");
                return;
            }

            var instance = Object.Instantiate(prefab);

            var entity = instance.GetComponent<NetworkEntity>();
            if (entity == null)
            {
                Debug.LogError($"No {nameof(NetworkEntity)} Found on Resource {command.Resource}");
                return;
            }

            entity.Spawn(command.Owner, command.Entity);
            Entities.Add(entity.ID, entity);

            if (Clients.TryGetValue(entity.Owner, out var client))
            {
                client.Entities.Add(entity);
            }
            else
            {
                Debug.Log($"Spawned Entity {entity.name} Has No Client Owner");
            }
        }

        void ClientDisconnected(ClientDisconnectPayload payload)
        {
            if (Clients.TryGetValue(payload.ID, out var client))
            {
                foreach (var entity in client.Entities)
                {
                    Entities.Remove(entity.ID);

                    GameObject.Destroy(entity.gameObject);
                }

                Clients.Remove(client.ID);
            }
            else
            {

            }
        }
        #endregion

        public NetworkRoom()
        {
            Clients = new Dictionary<NetworkClientID, NetworkClient>();

            Entities = new Dictionary<NetworkEntityID, NetworkEntity>();

            NetworkAPI.WebSocketAPI.OnConnect += ConnectCallback;
            NetworkAPI.WebSocketAPI.OnMessage += MessageCallback;
        }
    }
}