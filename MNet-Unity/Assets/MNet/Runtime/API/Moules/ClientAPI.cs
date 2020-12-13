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

            public static NetworkClient Self { get; private set; }
            public static NetworkClientID ID => Self.ID;

            public static bool IsConnected => Realtime.IsConnected;

            public static bool IsMaster
            {
                get
                {
                    if (Self == null) return false;

                    return Room.Master == Self;
                }
            }

            public static IReadOnlyList<NetworkEntity> Entities => Self?.Entities;

            public static MessageSendQueue SendQueue { get; private set; }

            public static void Configure()
            {

            }

            static void RealtimeInitializeCallback(NetworkTransport transport)
            {
                SendQueue = new MessageSendQueue(transport.CheckMTU);
            }

            static void Update()
            {
                if (IsConnected) Process();
            }

            static void Process()
            {
                if (AppAPI.Config.QueueMessages) SendQueue.Resolve(Realtime.Send);
            }

            public static bool Send<T>(T payload, DeliveryMode mode = DeliveryMode.Reliable)
            {
                if (IsConnected == false)
                {
                    Debug.LogWarning($"Cannot Send Payload '{payload}' When Network Client Isn't Connected");
                    return false;
                }

                var message = NetworkMessage.Write(payload);

                if (AppAPI.Config.QueueMessages)
                {
                    SendQueue.Add(message, mode);
                }
                else
                {
                    var binary = NetworkSerializer.Serialize(message);

                    Realtime.Send(binary, mode);
                }

                return true;
            }

            public delegate void ConnectDelegate();
            public static event ConnectDelegate OnConnect;
            static void ConnectCallback()
            {
                Debug.Log("Client Connected");

                if (AutoRegister) Register();

                OnConnect?.Invoke();
            }

            public delegate void MessageDelegate(NetworkMessage message, DeliveryMode mode);
            public static event MessageDelegate OnMessage;
            static void MessageCallback(NetworkMessage message, DeliveryMode mode)
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

                OnMessage?.Invoke(message, mode);
            }

            #region Register
            public static bool AutoRegister { get; set; } = true;

            public static bool IsRegistered => Self != null;

            public static void Register()
            {
                var request = new RegisterClientRequest(Profile);

                Send(request);
            }

            public delegate void RegisterDelegate(RegisterClientResponse response);
            public static event RegisterDelegate OnRegister;
            static void RegisterCallback(RegisterClientResponse response)
            {
                Self = new NetworkClient(response.ID, Profile);

                if (AutoReady) Ready();

                OnRegister?.Invoke(response);
            }
            #endregion

            #region Ready
            public static bool AutoReady { get; set; } = true;

            public static bool IsReady { get; private set; }

            public static void Ready()
            {
                var request = ReadyClientRequest.Write();

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
            public static void SpawnEntity(string resource, AttributesCollection attributes = null, NetworkClientID? owner = null)
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
                if (entity.Owner != Self) return;

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
                if (entity.Owner != Self) return;

                OnDestroyEntity?.Invoke(entity);
            }
            #endregion

            #region Disconnect
            public static void Disconnect() => Realtime.Disconnect();

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
                Self = null;

                IsReady = false;

                SendQueue.Clear();
            }

            static Client()
            {
                IsReady = false;

                Realtime.OnConnect += ConnectCallback;
                Realtime.OnMessage += MessageCallback;
                Realtime.OnDisconnect += DisconnectedCallback;
                Realtime.OnInitialize += RealtimeInitializeCallback;

                Room.OnSpawnEntity += SpawnEntityCallback;
                Room.OnDestroyEntity += DestroyEntityCallback;

                NetworkAPI.OnUpdate += Update;
            }
        }
    }
}