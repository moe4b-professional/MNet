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
            public static NetworkClient Self { get; private set; }

            public static NetworkClientID ID => Self.ID;
            public static NetworkClientProfile Profile => Self.Profile;

            public static bool IsConnected => Realtime.IsConnected;

            public static bool IsMaster
            {
                get
                {
                    if (Self == null) return false;

                    return Room.Master.Client == Self;
                }
            }

            public static bool IsRegistered => Register.IsComplete;
            public static bool IsReady => Ready.IsComplete;

            internal static void Configure()
            {
                MessageDispatcher.Configure();
                SendQueue.Configure();
                Register.Configure();
                Ready.Configure();
                Entities.Configure();
                RPR.Configure();

                Realtime.OnConnect += ConnectCallback;
                Realtime.OnMessage += MessageCallback;
                Realtime.OnDisconnect += DisconnectedCallback;
            }

            public static bool Send<T>(ref T payload, DeliveryMode mode = DeliveryMode.Reliable)
            {
                if (IsConnected == false)
                {
                    Debug.LogWarning($"Cannot Send Payload '{payload}' When Network Client Isn't Connected");
                    return false;
                }

                var message = NetworkMessage.Write(ref payload);

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

                OnConnect?.Invoke();
            }

            static void MessageCallback(NetworkMessage message, DeliveryMode mode)
            {
                MessageDispatcher.Invoke(message, mode);
            }

            public static class MessageDispatcher
            {
                static Dictionary<Type, MessageCallbackDelegate> Dictionary;

                internal static void Configure()
                {
                    Dictionary = new Dictionary<Type, MessageCallbackDelegate>();
                }

                internal static void Invoke(NetworkMessage message, DeliveryMode mode)
                {
                    if (Dictionary.TryGetValue(message.Type, out var callback) == false)
                    {
                        Debug.LogWarning($"Recieved Message with Payload of {message.Type} Has no Handler");
                        return;
                    }

                    callback(message);
                }

                public delegate void MessageCallbackDelegate(NetworkMessage message);
                public delegate void MessageHandlerDelegate<TPayload>(ref TPayload payload);

                public static void RegisterHandler<TPayload>(MessageHandlerDelegate<TPayload> handler)
                {
                    var type = typeof(TPayload);

                    if (Dictionary.ContainsKey(type)) throw new Exception($"Client Message Dispatcher Already Contains an Entry for {type}");

                    Dictionary.Add(type, Callback);

                    void Callback(NetworkMessage message)
                    {
                        var payload = message.Read<TPayload>();

                        handler(ref payload);
                    }
                }
            }

            public static class SendQueue
            {
                public static MessageSendQueue Queue { get; private set; }

                internal static void Configure()
                {
                    Realtime.OnInitialize += RealtimeInitializeCallback;

                    NetworkAPI.OnProcess += Process;
                }

                static void RealtimeInitializeCallback(NetworkTransport transport)
                {
                    Queue = new MessageSendQueue(transport.CheckMTU);
                }

                static void Process()
                {
                    if (IsConnected && AppAPI.Config.QueueMessages) Resolve();
                }

                static void Resolve()
                {
                    var deliveries = Queue.Deliveries;

                    for (int d = 0; d < deliveries.Count; d++)
                    {
                        if (deliveries[d].Empty) continue;

                        var buffers = deliveries[d].Read();

                        for (int b = 0; b < buffers.Count; b++)
                            Realtime.Send(buffers[b], deliveries[d].Mode);

                        deliveries[d].Clear();
                    }
                }

                public static void Add(byte[] raw, DeliveryMode mode) => Queue.Add(raw, mode);

                internal static void Clear()
                {
                    Queue.Clear();
                }
            }

            public static class Register
            {
                public static bool Auto { get; set; } = true;

                public static bool IsComplete => Self != null;

                public static NetworkClientProfile Profile { get; private set; }

                internal static void Configure()
                {
                    GetProfileMethod = DefaultGetProfileMethod;

                    OnConnect += ConnectCallback;

                    MessageDispatcher.RegisterHandler<RegisterClientResponse>(Callback);
                }

                static void ConnectCallback()
                {
                    if (Auto) Request();
                }

                public static void Request()
                {
                    Profile = GetProfileMethod();

                    var request = new RegisterClientRequest(Profile);

                    Send(ref request);
                }

                public delegate NetworkClientProfile ProfileDelegate();
                public static ProfileDelegate GetProfileMethod { get; set; }
                static NetworkClientProfile DefaultGetProfileMethod()
                {
                    var name = $"Player {Random.Range(0, 1000)}";

                    return new NetworkClientProfile(name);
                }

                public delegate void callbackDelegate(RegisterClientResponse response);
                public static event callbackDelegate OnCallback;
                static void Callback(ref RegisterClientResponse response)
                {
                    Self = new NetworkClient(response.ID, Profile);

                    Debug.Log("Client Registered");

                    OnCallback?.Invoke(response);
                }

                internal static void Clear()
                {

                }
            }

            public static class Ready
            {
                public static bool Auto { get; set; } = true;

                public static bool IsComplete { get; private set; }

                internal static void Configure()
                {
                    IsComplete = false;

                    Register.OnCallback += RegisterCallback;

                    MessageDispatcher.RegisterHandler<ReadyClientResponse>(Callback);
                }

                static void RegisterCallback(RegisterClientResponse response)
                {
                    if (Auto) Request();
                }

                public static void Request()
                {
                    var request = ReadyClientRequest.Write();

                    Send(ref request);
                }

                public delegate void CallbackDelegate(ReadyClientResponse response);
                public static event CallbackDelegate OnCallback;
                static void Callback(ref ReadyClientResponse response)
                {
                    IsComplete = true;

                    Debug.Log("Client Set Ready");

                    OnCallback?.Invoke(response);
                }

                internal static void Clear()
                {
                    IsComplete = false;
                }
            }

            public static class Entities
            {
                public static IReadOnlyList<NetworkEntity> List => Self?.Entities;

                internal static void Configure()
                {
                    Tokens = new AutoKeyDictionary<EntitySpawnToken, NetworkEntity>(EntitySpawnToken.Increment);

                    MessageDispatcher.RegisterHandler<SpawnEntityResponse>(SpawnResponse);

                    Room.Entities.OnSpawn += SpawnCallback;
                    Room.Entities.OnDestroy += DestroyCallback;
                }

                internal static void Clear()
                {
                    Tokens.Clear();
                }

                #region Spawn
                static AutoKeyDictionary<EntitySpawnToken, NetworkEntity> Tokens;

                public static NetworkEntity Spawn(GameObject prefab, PersistanceFlags persistance = PersistanceFlags.None, AttributesCollection attributes = null, NetworkClientID? owner = null)
                {
                    if (NetworkAPI.Config.SpawnableObjects.TryGetIndex(prefab, out var resource) == false)
                        throw new Exception($"Prefab '{prefab}' Not Registerd as a Network Spawnable Object");

                    return Spawn(resource, persistance: persistance, attributes: attributes, owner: owner);
                }

                public static NetworkEntity Spawn(string name, PersistanceFlags persistance = PersistanceFlags.None, AttributesCollection attributes = null, NetworkClientID? owner = null)
                {
                    if (NetworkAPI.Config.SpawnableObjects.TryGetIndex(name, out var resource) == false)
                        throw new Exception($"No Network Spawnable Objects Registerd with Name '{name}'");

                    return Spawn(resource, persistance: persistance, attributes: attributes, owner: owner);
                }

                public static NetworkEntity Spawn(ushort resource, PersistanceFlags persistance = PersistanceFlags.None, AttributesCollection attributes = null, NetworkClientID? owner = null)
                {
                    var token = Tokens.Reserve();

                    var instance = Room.Entities.Instantiate(resource);

                    instance.Setup(Self, EntityType.Dynamic, persistance, attributes);

                    Tokens.Assign(token, instance);

                    var request = SpawnEntityRequest.Write(resource, token, persistance, attributes, owner);

                    Send(ref request);

                    return instance;
                }

                static void SpawnResponse(ref SpawnEntityResponse payload)
                {
                    if (Tokens.TryGetValue(payload.Token, out var entity) == false)
                    {
                        Debug.LogError($"Couldn't Find Entity with Token {payload.Token} to Finish Spawining");
                        return;
                    }

                    Tokens.Remove(payload.Token);

                    Room.Entities.SpawnLocal(entity, payload.ID);
                }

                public delegate void SpawnDelegate(NetworkEntity entity);
                public static event SpawnDelegate OnSpawn;
                static void SpawnCallback(NetworkEntity entity)
                {
                    if (entity.Owner != Self) return;

                    OnSpawn?.Invoke(entity);
                }
                #endregion

                #region Destroy
                public static void Destroy(NetworkEntity entity) => Destroy(entity.ID);
                public static void Destroy(NetworkEntityID id)
                {
                    var request = new DestroyEntityRequest(id);

                    Send(ref request);
                }

                public delegate void DestroyDelegate(NetworkEntity entity);
                public static event DestroyDelegate OnDestroy;
                static void DestroyCallback(NetworkEntity entity)
                {
                    if (entity.Owner != Self) return;

                    OnDestroy?.Invoke(entity);
                }
                #endregion
            }

            public static class RPR
            {
                static AutoKeyDictionary<RprChannelID, RprPromise> promises;

                internal static void Configure()
                {
                    promises = new AutoKeyDictionary<RprChannelID, RprPromise>(RprChannelID.Increment);

                    Client.MessageDispatcher.RegisterHandler<RprCommand>(Command);
                    Client.MessageDispatcher.RegisterHandler<RprResponse>(Response);

                    Room.Clients.OnRemove += RemoveClientCallback;
                }

                static void Response(ref RprResponse response)
                {
                    if (promises.TryGetValue(response.Channel, out var promise) == false)
                    {
                        Debug.LogWarning($"Recieved RPR Response for Channel {response.Channel} But that Command Doesn't Exist");
                        return;
                    }

                    if (Room.Clients.TryGet(response.Sender, out var sender) == false)
                    {
                        Debug.LogWarning($"Recieved RPR Response for Channel {response.Channel} from Unregistered Client {response.Sender}");
                        return;
                    }

                    if (sender != promise.Target)
                    {
                        Debug.LogWarning($"Recieved RPR Response for Channel {response.Channel} from Client {sender} but That RPR was Targeted towards {promise.Target}");
                        return;
                    }

                    Fullfil(promise, response.Response, response.Raw);
                }

                static void Command(ref RprCommand command)
                {
                    if (promises.TryGetValue(command.Channel, out var promise) == false)
                    {
                        Debug.LogWarning($"Recieved RPR Command for Channel {command.Channel} But that Command Doesn't Exist");
                        return;
                    }

                    Fullfil(promise, command.Response, null);
                }

                static void RemoveClientCallback(NetworkClient client)
                {
                    var selection = promises.Values.Where(IsClient).ToArray();

                    foreach (var promise in selection) Fullfil(promise, RemoteResponseType.Disconnect, null);

                    bool IsClient(RprPromise promise) => promise.Target == client;
                }

                static void Fullfil(RprPromise promise, RemoteResponseType response, byte[] raw)
                {
                    promise.Fullfil(response, raw);
                    promises.Remove(promise.Channel);
                }

                public static RprPromise Promise(NetworkClient target)
                {
                    var channel = promises.Reserve();

                    var promise = new RprPromise(target, channel);

                    promises.Assign(channel, promise);

                    return promise;
                }

                #region Respond
                public static bool Respond(RpcCommand command, RemoteResponseType response) => Respond(command.Sender, command.ReturnChannel, response);

                public static bool Respond(NetworkClientID target, RprChannelID channel, object value, Type type)
                {
                    var request = RprRequest.Write(target, channel, value, type);

                    if(target == Client.ID)
                    {
                        var response = RprResponse.Write(Client.ID, request);
                        Response(ref response);
                        return true;
                    }

                    return Send(ref request);
                }

                public static bool Respond(NetworkClientID target, RprChannelID channel, RemoteResponseType response)
                {
                    var request = RprRequest.Write(target, channel, response);
                    return Send(ref request);
                }
                #endregion
            }

            public delegate void DisconnectDelegate(DisconnectCode code);
            public static event DisconnectDelegate OnDisconnect;
            static void DisconnectedCallback(DisconnectCode code)
            {
                Debug.Log($"Client Disconnected, Code: {code}");

                Clear();

                OnDisconnect?.Invoke(code);
            }

            public static void Disconnect() => Realtime.Disconnect();

            static void Clear()
            {
                Self = default;

                SendQueue.Clear();
                Register.Clear();
                Ready.Clear();
                Entities.Clear();
            }
        }
    }
}