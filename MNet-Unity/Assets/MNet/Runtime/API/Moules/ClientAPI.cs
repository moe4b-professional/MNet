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

namespace MNet
{
    public static partial class NetworkAPI
    {
        public static class Client
        {
            public static NetworkClientProfile Profile { get; set; }

            public static NetworkClient Instance { get; private set; }
            public static NetworkClientID ID => Instance.ID;

            public static bool IsConnected => RealtimeAPI.IsConnected;

            public static bool IsMaster
            {
                get
                {
                    if (Instance == null) return false;

                    return Room.Master == Instance;
                }
            }

            public static IReadOnlyList<NetworkEntity> Entities => Instance?.Entities;

            public static void Configure()
            {

            }

            public static bool Send<T>(T payload)
            {
                var message = NetworkMessage.Write(payload);

                var raw = NetworkSerializer.Serialize(message);

                return RealtimeAPI.Send(raw);
            }

            public delegate void ConnectDelegate();
            public static event ConnectDelegate OnConnect;
            static void ConnectCallback()
            {
                Debug.Log("Client Connected");

                if (AutoRegister) Register();

                OnConnect?.Invoke();
            }

            public delegate void MessageDelegate(NetworkMessage message);
            public static event MessageDelegate OnMessage;
            static void MessageCallback(NetworkMessage message)
            {
                if (message.Is<RegisterClientResponse>())
                {
                    var response = message.Read<RegisterClientResponse>();

                    RegisterCallback(response);
                }
                else if (message.Is<ReadyClientResponse>())
                {
                    var response = message.Read<ReadyClientResponse>();

                    ReadyCallback(response);
                }

                OnMessage?.Invoke(message);
            }

            #region Register
            public static bool AutoRegister { get; set; } = true;

            public static bool IsRegistered => Instance != null;

            public static void Register()
            {
                var request = new RegisterClientRequest(Profile);

                Send(request);
            }

            public delegate void RegisterDelegate(RegisterClientResponse response);
            public static event RegisterDelegate OnRegister;
            static void RegisterCallback(RegisterClientResponse response)
            {
                Instance = new NetworkClient(response.ID, Profile);

                if (AutoReady) Ready();

                OnRegister?.Invoke(response);
            }
            #endregion

            #region Ready
            public static bool AutoReady { get; set; } = true;

            public static bool IsReady { get; private set; }

            public static void Ready()
            {
                var request = new ReadyClientRequest();

                Send(request);
            }

            public delegate void ReadyDelegate(ReadyClientResponse response);
            public static event ReadyDelegate OnReady;
            static void ReadyCallback(ReadyClientResponse response)
            {
                IsReady = true;

                OnReady?.Invoke(response);
            }
            #endregion

            #region Spawn Entity
            public static void SpawnEntity(string resource, NetworkClientID? owner = null) => SpawnEntity(resource, null, owner);
            public static void SpawnEntity(string resource, AttributesCollection attributes, NetworkClientID? owner = null)
            {
                var request = SpawnEntityRequest.Write(resource, attributes, owner);

                Send(request);
            }

            public static void SpawnSceneObject(NetworkEntity entity, ushort index) => SpawnSceneObject(entity.Scene, index);
            public static void SpawnSceneObject(Scene scene, ushort index) => SpawnSceneObject((byte)scene.buildIndex, index);
            public static void SpawnSceneObject(byte scene, ushort index)
            {
                if (IsMaster == false)
                {
                    Debug.LogError("Only the Master Client May Spawn Scene Objects, Ignoring Request");
                    return;
                }

                var request = SpawnEntityRequest.Write(scene, index);

                Send(request);
            }

            public delegate void SpawnEntityDelegate(NetworkEntity entity);
            public static event SpawnEntityDelegate OnSpawnEntity;
            static void SpawnEntityCallback(NetworkEntity entity)
            {
                if (entity.Owner != Instance) return;

                OnSpawnEntity?.Invoke(entity);
            }
            #endregion

            #region Destroy Entity
            public static void DestroyEntity(NetworkEntity entity) => DestroyEntity(entity.ID);
            public static void DestroyEntity(NetworkEntityID id)
            {
                var request = new DestroyEntityRequest(id);

                Send(request);
            }

            public delegate void DestroyEntityDelegate(NetworkEntity entity);
            public static event DestroyEntityDelegate OnDestroyEntity;
            static void DestroyEntityCallback(NetworkEntity entity)
            {
                if (entity.Owner != Instance) return;

                OnDestroyEntity?.Invoke(entity);
            }
            #endregion

            #region Disconnect
            public static void Disconnect() => RealtimeAPI.Disconnect();

            public delegate void DisconnectDelegate(DisconnectCode code);
            public static event DisconnectDelegate OnDisconnect;
            static void DisconnectedCallback(DisconnectCode code)
            {
                Debug.Log($"Client Disconnected, Code: {code}");

                Clear();

                OnDisconnect?.Invoke(code);
            }
            #endregion

            static void Clear()
            {
                Instance = null;

                IsReady = false;
            }

            static Client()
            {
                RealtimeAPI.OnConnect += ConnectCallback;
                RealtimeAPI.OnMessage += MessageCallback;
                RealtimeAPI.OnDisconnect += DisconnectedCallback;

                Room.OnSpawnEntity += SpawnEntityCallback;
                Room.OnDestroyEntity += DestroyEntityCallback;

                IsReady = false;
            }
        }
    }
}