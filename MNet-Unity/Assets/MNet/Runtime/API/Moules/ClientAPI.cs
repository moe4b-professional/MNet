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

            #region Message Dispatcher
            internal static Dictionary<Type, MessageCallbackDelegate> MessageDispatcher { get; private set; }

            public delegate void MessageCallbackDelegate(NetworkMessage message);
            public delegate void MessageHandlerDelegate<TPayload>(TPayload payload);

            public static void RegisterMessageHandler<TPayload>(MessageHandlerDelegate<TPayload> handler)
            {
                var type = typeof(TPayload);

                if (MessageDispatcher.ContainsKey(type)) throw new Exception($"Client Message Dispatcher Already Contains an Entry for {type}");

                MessageDispatcher.Add(type, Callback);

                void Callback(NetworkMessage message)
                {
                    var payload = message.Read<TPayload>();

                    handler(payload);
                }
            }
            #endregion

            internal static void Configure()
            {
                IsReady = false;

                MessageDispatcher = new Dictionary<Type, MessageCallbackDelegate>();

                RegisterMessageHandler<RegisterClientResponse>(RegisterCallback);
                RegisterMessageHandler<ReadyClientResponse>(ReadyCallback);

                NetworkAPI.OnProcess += Process;

                Realtime.OnConnect += ConnectCallback;
                Realtime.OnMessage += MessageCallback;
                Realtime.OnDisconnect += DisconnectedCallback;
                Realtime.OnInitialize += RealtimeInitializeCallback;

                Room.OnSpawnEntity += SpawnEntityCallback;
                Room.OnDestroyEntity += DestroyEntityCallback;
            }

            static void RealtimeInitializeCallback(NetworkTransport transport)
            {
                SendQueue = new MessageSendQueue(transport.CheckMTU);
            }

            static void Process()
            {
                if (IsConnected && AppAPI.Config.QueueMessages) ResolveSendQueue();
            }

            static void ResolveSendQueue()
            {
                var deliveries = SendQueue.Deliveries;

                for (int d = 0; d < deliveries.Count; d++)
                {
                    if (deliveries[d].Empty) continue;

                    var buffers = deliveries[d].Read();

                    for (int b = 0; b < buffers.Count; b++)
                        Realtime.Send(buffers[b], deliveries[d].Mode);

                    deliveries[d].Clear();
                }
            }

            public static bool Send<T>(T payload, DeliveryMode mode = DeliveryMode.Reliable)
            {
                if (IsConnected == false)
                {
                    Debug.LogWarning($"Cannot Send Payload '{payload}' When Network Client Isn't Connected");
                    return false;
                }

                var message = NetworkMessage.Write(payload);

                var raw = NetworkSerializer.Serialize(message);

                if (AppAPI.Config.QueueMessages)
                    SendQueue.Add(raw, mode);
                else
                    Realtime.Send(raw, mode);

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

            static void MessageCallback(NetworkMessage message, DeliveryMode mode)
            {
                if (MessageDispatcher.TryGetValue(message.Type, out var callback) == false)
                {
                    Debug.LogWarning($"Recieved Message with Payload of {message.Type} Has no Handler");
                    return;
                }

                callback(message);
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

                Debug.Log("Client Registered");

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

                Debug.Log("Client Set Ready");

                OnReady?.Invoke(response);
            }
            #endregion

            #region Spawn Entity
            public static void SpawnEntity(GameObject prefab, PersistanceFlags persistance = PersistanceFlags.None, AttributesCollection attributes = null, NetworkClientID? owner = null)
            {
                if (NetworkAPI.SpawnableObjects.Prefabs.TryGetValue(prefab, out var resource) == false)
                {
                    Debug.LogError($"Prefab '{prefab}' Not Registerd as a Network Spawnable Object");
                    return;
                }

                SpawnEntity(resource, persistance : persistance, attributes: attributes, owner: owner);
            }

            public static void SpawnEntity(string name, PersistanceFlags persistance = PersistanceFlags.None, AttributesCollection attributes = null, NetworkClientID? owner = null)
            {
                if (NetworkAPI.SpawnableObjects.Names.TryGetValue(name, out var resource) == false)
                {
                    Debug.LogError($"No Network Spawnable Objects Registerd with Name '{name}'");
                    return;
                }

                SpawnEntity(resource, persistance: persistance, attributes: attributes, owner: owner);
            }

            public static void SpawnEntity(ushort resource, PersistanceFlags persistance = PersistanceFlags.None, AttributesCollection attributes = null, NetworkClientID? owner = null)
            {
                var request = SpawnEntityRequest.Write(resource, persistance, attributes, owner);

                Send(request);
            }

            #region Scene Object
            internal static void SpawnSceneObject(ushort resource, Scene scene) => SpawnSceneObject(resource, (byte)scene.buildIndex);

            internal static void SpawnSceneObject(ushort resource, byte scene)
            {
                if (IsMaster == false)
                {
                    Debug.LogError("Only the Master Client May Spawn Scene Objects, Ignoring Request");
                    return;
                }

                var request = SpawnEntityRequest.Write(resource, scene);

                Send(request);
            }
            #endregion

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
        }
    }
}