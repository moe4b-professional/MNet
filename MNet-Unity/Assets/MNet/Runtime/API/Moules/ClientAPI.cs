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

            public static bool IsRegistered => Register.IsComplete;

            public static bool IsMaster
            {
                get
                {
                    if (Self == null) return false;

                    return Self.IsMaster;
                }
            }

            internal static void Configure()
            {
                MessageDispatcher.Configure();
                SendQueue.Configure();
                Prediction.Clear();
                Register.Configure();
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

                if (Prediction.Process(ref payload, mode) == Prediction.Response.Consume) return true;

                if (OfflineMode.On)
                {
                    Debug.LogWarning($"Payload of Type {payload.GetType()} not Consumed in Offline Mode");
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

                internal static void Invoke<TPayload>(ref TPayload payload, DeliveryMode mode)
                {
                    var message = NetworkMessage.Write(ref payload);

                    Invoke(message, mode);
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
                    NetworkAPI.OnProcess += Process;

                    Realtime.OnInitialize += Initialize;
                }

                static void Initialize(NetworkTransport transport)
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

            public static class Prediction
            {
                public enum Response
                {
                    None, Consume, Send
                }

                internal static void Configure()
                {

                }

                internal static Response Process<TPayload>(ref TPayload payload, DeliveryMode mode)
                {
                    switch (payload)
                    {
                        case RegisterClientRequest instance:
                            return RegisterClient(ref instance, mode);

                        case SpawnEntityRequest instance:
                            return SpawnEntity(ref instance, mode);

                        case TransferEntityPayload instance:
                            return TransferEntity(ref instance, mode);

                        case TakeoverEntityRequest instance:
                            return TakeoverEntity(ref instance, mode);

                        case DestroyEntityPayload instance:
                            return DestroyEntity(ref instance, mode);

                        case RpcRequest instance:
                            return InvokeRPC(ref instance, mode);

                        case RprRequest instance:
                            return InvokeRPR(ref instance, mode);

                        case SyncVarRequest instance:
                            return InvokeSyncVar(ref instance, mode);

                        case LoadScenesPayload instance:
                            return LoadScenes(ref instance, mode);

                        case ChangeRoomInfoPayload instance:
                            return ChangeRoomInfo(ref instance, mode);

                        case PingRequest instance:
                            return Ping(ref instance, mode);

                        case TimeRequest instance:
                            return Time(ref instance, mode);
                    }

                    return Response.None;
                }

                static Response RegisterClient(ref RegisterClientRequest request, DeliveryMode mode)
                {
                    if (OfflineMode.On)
                    {
                        var id = new NetworkClientID();
                        var clients = new NetworkClientInfo[] { new NetworkClientInfo(id, Register.Profile) };
                        var buffer = new NetworkMessage[] { };
                        var time = TimeResponse.Write(default, request.Time);

                        var response = new RegisterClientResponse(id, OfflineMode.RoomInfo, clients, id, buffer, time);

                        MessageDispatcher.Invoke(ref response, mode);

                        return Response.Consume;
                    }

                    return Response.Send;
                }

                #region Entity
                static Response SpawnEntity(ref SpawnEntityRequest request, DeliveryMode mode)
                {
                    if (OfflineMode.On)
                    {
                        var id = OfflineMode.EntityIDs.Reserve();

                        if (request.Type == EntityType.Dynamic)
                        {
                            var response = SpawnEntityResponse.Write(id, request.Token);

                            MessageDispatcher.Invoke(ref response, mode);
                        }
                        else
                        {
                            var command = SpawnEntityCommand.Write(Client.ID, id, request);

                            MessageDispatcher.Invoke(ref command, mode);
                        }

                        return Response.Consume;
                    }

                    return Response.Send;
                }

                static Response TransferEntity(ref TransferEntityPayload payload, DeliveryMode mode)
                {
                    MessageDispatcher.Invoke(ref payload, mode);

                    if (OfflineMode.On) return Response.Consume;

                    return Response.Send;
                }

                static Response TakeoverEntity(ref TakeoverEntityRequest request, DeliveryMode mode)
                {
                    var command = TakeoverEntityCommand.Write(Client.ID, request);

                    MessageDispatcher.Invoke(ref command, mode);

                    if (OfflineMode.On) return Response.Consume;

                    return Response.Send;
                }

                static Response DestroyEntity(ref DestroyEntityPayload request, DeliveryMode mode)
                {
                    MessageDispatcher.Invoke(ref request, DeliveryMode.Reliable);

                    if (OfflineMode.On)
                    {
                        OfflineMode.EntityIDs.Free(request.ID);

                        return Response.Consume;
                    }

                    return Response.Send;
                }
                #endregion

                #region RPC
                static Response InvokeRPC(ref RpcRequest request, DeliveryMode mode)
                {
                    switch (request.Type)
                    {
                        case RpcType.Broadcast:
                            return InvokeBroadcastRPC(ref request, mode);

                        case RpcType.Target:
                        case RpcType.Query:
                            return InvokeDirectRPC(ref request, mode);

                        default:
                            throw new NotImplementedException();
                    }
                }

                static Response InvokeBroadcastRPC(ref RpcRequest request, DeliveryMode mode)
                {
                    if (request.Exception != Client.ID)
                    {
                        var command = RpcCommand.Write(Client.ID, request);

                        MessageDispatcher.Invoke(ref command, mode);
                    }

                    if (OfflineMode.On) return Response.Consume;

                    return Response.Send;
                }

                static Response InvokeDirectRPC(ref RpcRequest request, DeliveryMode mode)
                {
                    if (request.Target == Client.ID)
                    {
                        var command = RpcCommand.Write(Client.ID, request);

                        MessageDispatcher.Invoke(ref command, mode);

                        return Response.Consume;
                    }

                    if (OfflineMode.On)
                    {
                        Debug.LogWarning($"Invoking {request.Type} RPC on Non-Local Client {request.Target} in Offline Mode!");
                        return Response.Consume;
                    }

                    return Response.Send;
                }
                #endregion

                static Response InvokeRPR(ref RprRequest request, DeliveryMode mode)
                {
                    if (request.Target == Client.ID)
                    {
                        var response = RprResponse.Write(Client.ID, request);

                        MessageDispatcher.Invoke(ref response, mode);

                        return Response.Consume;
                    }

                    if (OfflineMode.On)
                    {
                        Debug.LogWarning($"Invoking RPR on Non-Local Client {request.Target} in Offline Mode!");
                        return Response.Consume;
                    }

                    return Response.Send;
                }

                static Response InvokeSyncVar(ref SyncVarRequest request, DeliveryMode mode)
                {
                    var command = SyncVarCommand.Write(Client.ID, request);

                    MessageDispatcher.Invoke(ref command, mode);

                    if (OfflineMode.On) return Response.Consume;

                    return Response.Send;
                }

                static Response LoadScenes(ref LoadScenesPayload payload, DeliveryMode mode)
                {
                    MessageDispatcher.Invoke(ref payload, mode);

                    if (OfflineMode.On) return Response.Consume;

                    return Response.Send;
                }

                static Response ChangeRoomInfo(ref ChangeRoomInfoPayload payload, DeliveryMode mode)
                {
                    MessageDispatcher.Invoke(ref payload, mode);

                    if (OfflineMode.On) return Response.Consume;

                    return Response.Send;
                }

                static Response Ping(ref PingRequest request, DeliveryMode mode)
                {
                    if (OfflineMode.On)
                    {
                        var response = new PingResponse(request);

                        MessageDispatcher.Invoke(ref response, mode);

                        return Response.Consume;
                    }

                    return Response.Send;
                }

                static Response Time(ref TimeRequest request, DeliveryMode mode)
                {
                    if (OfflineMode.On)
                    {
                        var response = new TimeResponse(NetworkAPI.Time.Span, request.Timestamp);

                        MessageDispatcher.Invoke(ref response, mode);

                        return Response.Consume;
                    }

                    return Response.Send;
                }

                internal static void Clear()
                {

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

                    var request = RegisterClientRequest.Write(Profile);

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

                    Room.Register(ref response);

                    OnCallback?.Invoke(response);
                }

                internal static void Clear()
                {

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
                    if (NetworkAPI.Config.SyncedAssets.TryGetIndex(prefab, out var resource) == false)
                        throw new Exception($"Prefab '{prefab}' Not Registerd as a Network Spawnable Object");

                    return Spawn(resource, persistance: persistance, attributes: attributes, owner: owner);
                }

                public static NetworkEntity Spawn(ushort resource, PersistanceFlags persistance = PersistanceFlags.None, AttributesCollection attributes = null, NetworkClientID? owner = null)
                {
                    if (owner != null && IsMaster == false)
                    {
                        Debug.LogError($"Only the Master Client can Spawn Entities for other Clients");
                        return null;
                    }

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
                public static void Destroy(NetworkBehaviour behaviour) => Destroy(behaviour.Entity);
                public static void Destroy(NetworkEntity entity)
                {
                    if (entity.IsReady == false)
                    {
                        Debug.LogError($"Can't Destory Entity {entity} Because it's still not Ready");
                        return;
                    }

                    if (entity.CheckAuthority(Client.Self) == false)
                    {
                        Debug.LogError($"Cannot Destory Entity {entity} Because Local Client doesn't have Authority over that Entity");
                        return;
                    }

                    var request = new DestroyEntityPayload(entity.ID);

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

            internal static class RPR
            {
                static AutoKeyDictionary<RprChannelID, RprPromise> promises;

                internal static void Configure()
                {
                    promises = new AutoKeyDictionary<RprChannelID, RprPromise>(RprChannelID.Increment);

                    Client.MessageDispatcher.RegisterHandler<RprCommand>(Fullfil);
                    Client.MessageDispatcher.RegisterHandler<RprResponse>(Fullfil);

                    Room.Clients.OnRemove += ClientRemovedCallback;
                }

                static void ClientRemovedCallback(NetworkClient client)
                {
                    var selection = promises.Values.Where(IsClient).ToArray();

                    foreach (var promise in selection) Fullfil(promise, RemoteResponseType.Disconnect, null);

                    bool IsClient(RprPromise promise) => promise.Target == client;
                }

                #region Fullfil
                static void Fullfil(ref RprCommand command)
                {
                    if (promises.TryGetValue(command.Channel, out var promise) == false)
                    {
                        Debug.LogWarning($"Recieved RPR Command for Channel {command.Channel} But that Command Doesn't Exist");
                        return;
                    }

                    Fullfil(promise, command.Response, null);
                }

                static void Fullfil(ref RprResponse response)
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

                static void Fullfil(RprPromise promise, RemoteResponseType response, byte[] raw)
                {
                    promise.Fullfil(response, raw);
                    promises.Remove(promise.Channel);
                }
                #endregion

                internal static RprPromise Promise(NetworkClient target)
                {
                    var channel = promises.Reserve();

                    var promise = new RprPromise(target, channel);

                    promises.Assign(channel, promise);

                    return promise;
                }

                #region Respond
                internal static bool Respond(RpcCommand command, RemoteResponseType response)
                {
                    if (response == RemoteResponseType.Success)
                    {
                        Log.Error($"Cannot Respond to RPR with Success Without Providing a Value");
                        return false;
                    }

                    var request = RprRequest.Write(command.Sender, command.ReturnChannel, response);
                    return Send(ref request);
                }

                internal static bool Respond(RpcCommand command, object value, Type type)
                {
                    var request = RprRequest.Write(command.Sender, command.ReturnChannel, value, type);
                    return Send(ref request);
                }
                #endregion
            }

            public static void Disconnect() => Realtime.Disconnect();

            public delegate void DisconnectDelegate(DisconnectCode code);
            public static event DisconnectDelegate OnDisconnect;
            static void DisconnectedCallback(DisconnectCode code)
            {
                Debug.Log($"Client Disconnected, Code: {code}");

                Clear();

                OnDisconnect?.Invoke(code);
            }

            static void Clear()
            {
                Self = default;

                SendQueue.Clear();
                Prediction.Clear();
                Register.Clear();
                Entities.Clear();

                NetworkAPI.Room.Clear();
            }
        }
    }
}