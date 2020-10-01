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

        public static class Client
        {
            public static NetworkClientProfile Profile { get; set; }

            public static NetworkClient Instance { get; private set; }

            public static NetworkClientID ID => Instance.ID;

            public static bool IsConnected => RealtimeAPI.IsConnected;

            public static bool IsReady { get; private set; }

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

            public static NetworkMessage Send<T>(T payload)
            {
                var message = NetworkMessage.Write(payload);

                var raw = NetworkSerializer.Serialize(message);

                RealtimeAPI.Send(raw);

                return message;
            }

            public delegate void ConnectDelegate();
            public static event ConnectDelegate OnConnect;
            static void ConnectCallback()
            {
                Debug.Log("Client Connected");

                if (AutoReady) RequestRegister();

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

            public static void RequestRegister()
            {
                var request = new RegisterClientRequest(Profile);

                Send(request);
            }

            public static event Action OnRegister;
            static void RegisterCallback(RegisterClientResponse response)
            {
                Instance = new NetworkClient(response.ID, Profile);

                if (AutoReady) RequestReady();

                OnRegister?.Invoke();
            }
            #endregion

            #region Ready
            public static bool AutoReady { get; set; } = true;

            public static void RequestReady()
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
            public static void RequestSpawnEntity(string resource) => RequestSpawnEntity(resource, null);
            public static void RequestSpawnEntity(string resource, AttributesCollection attributes)
            {
                var request = SpawnEntityRequest.Write(resource, attributes);

                Send(request);
            }

            public static void RequestSpawnEntity(NetworkEntity entity, int index) => RequestSpawnEntity(entity.Scene, index);
            public static void RequestSpawnEntity(Scene scene, int index) => RequestSpawnEntity(scene.buildIndex, index);
            public static void RequestSpawnEntity(int scene, int index)
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

            #region Destory Entity
            public static void RequestDestoryEntity(NetworkEntity entity) => RequestDestoryEntity(entity.ID);
            public static void RequestDestoryEntity(NetworkEntityID id)
            {
                var request = new DestroyEntityRequest(id);

                Send(request);
            }

            public delegate void DestoryEntityDelegate(NetworkEntity entity);
            public static event DestoryEntityDelegate OnDestoryEntity;
            static void DestoryEntityCallback(NetworkEntity entity)
            {
                if (entity.Owner != Instance) return;

                OnDestoryEntity?.Invoke(entity);
            }
            #endregion

            public static void Disconnect() => RealtimeAPI.Disconnect();

            public delegate void DisconnectDelegate();
            public static event DisconnectDelegate OnDisconnect;
            static void DisconnectedCallback()
            {
                Debug.Log($"Client Disconnected");

                Clear();

                OnDisconnect?.Invoke();
            }

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
                Room.OnDestoryEntity += DestoryEntityCallback;

                IsReady = false;
            }
        }
    }
}