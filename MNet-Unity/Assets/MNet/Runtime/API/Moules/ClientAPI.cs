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
using Cysharp.Threading.Tasks;
using MB;
using System.Runtime.CompilerServices;

namespace MNet
{
    public static partial class NetworkAPI
    {
        public static class Client
        {
            public static NetworkClient Self { get; private set; }
            public static NetworkClientID ID => Self.ID;

            public static NetworkClientProfile Profile { get; private set; }

            public static string Name
            {
                get => Profile.Name;
                set => Profile.Name = value;
            }

            public static bool IsConnected => Realtime.IsConnected;
            public static bool IsRegistered => Register.IsComplete;

            public static bool IsMaster => Self == null ? false : Self.IsMaster;

            static NetworkStream NetworkReader;
            static NetworkStream NetworkWriter;

            internal static void Configure()
            {
                NetworkReader = new NetworkStream();
                NetworkWriter = new NetworkStream(1024);

                Profile = new NetworkClientProfile("Player");

                MessageDispatcher.Configure();
                Prediction.Clear();
                Register.Configure();
                Groups.Configure();
                Entities.Configure();
                RPR.Configure();
                System.Configure();
                Buffer.Configure();

                Realtime.OnConnect += ConnectCallback;
                Realtime.OnMessage += MessageCallback;
                Realtime.OnDisconnect += DisconnectedCallback;

                MessageDispatcher.RegisterHandler<ServerLogPayload>(ServerLogHandler);
            }

            static void ServerLogHandler(ref ServerLogPayload payload)
            {
                Log.Add($"Server Log: {payload.Text}", payload.Level);
            }

            public static bool Send<T>(ref T payload, DeliveryMode mode = DeliveryMode.ReliableOrdered, byte channel = 0)
            {
                if (IsConnected == false)
                {
                    Debug.LogWarning($"Cannot Send Payload '{payload}' When Network Client Isn't Connected");
                    return false;
                }

                if (Prediction.Process(ref payload, mode) == Prediction.Response.Consume)
                    return true;

                if (OfflineMode.On)
                {
                    Debug.LogWarning($"Payload of Type {payload.GetType()} not Consumed in Offline Mode");
                    return false;
                }

                using (NetworkWriter)
                {
                    NetworkWriter.Write(typeof(T));
                    NetworkWriter.Write(payload);

                    var segment = NetworkWriter.ToSegment();
                    Realtime.Send(segment, mode, channel);
                }

                return true;
            }

            static void ConnectCallback()
            {
                Debug.Log("Client Connected");

                Register.Request();
            }

            public delegate void ReadyDelegate();
            public static event ReadyDelegate OnReady;
            internal static void ReadyCallback(ref RegisterClientResponse response)
            {
                Debug.Log("Client Ready");

                Self = new NetworkClient(response.ID, Profile);

                Room.Register(ref response);

                OnReady?.Invoke();
            }

            static void MessageCallback(ArraySegment<byte> segment, DeliveryMode mode)
            {
                using (NetworkReader.Assign(segment))
                {
                    var type = NetworkReader.Read<Type>();
                    MessageDispatcher.Invoke(type, NetworkReader);
                }
            }

            /// <summary>
            /// Class responsible for applying room buffers for late joining clients
            /// </summary>
            public static class Buffer
            {
                public static bool IsOn { get; private set; } = false;

                public delegate void BufferDelegate();

                public static event BufferDelegate OnBegin;

                static NetworkStream NetworkReader;

                internal static void Configure()
                {
                    NetworkReader = new NetworkStream();
                }

                internal static void Apply(ArraySegment<byte> buffer)
                {
                    if (IsOn) throw new Exception($"Cannot Apply Multiple Buffers at the Same Time");

                    IsOn = true;
                    OnBegin?.Invoke();

                    if (OfflineMode.On && OfflineMode.Scene.HasValue)
                    {
                        var payload = new LoadScenePayload(OfflineMode.Scene.Value, NetworkSceneLoadMode.Single);
                        MessageDispatcher.Invoke(payload);
                    }

                    NetworkAPI.OnProcess += Process;

                    if (buffer.Count == 0)
                        End();
                    else
                        NetworkReader.Assign(buffer);
                }

                static void Process()
                {
                    while (true)
                    {
                        if (Realtime.Pause.Active)
                            break;

                        var type = NetworkReader.Read<Type>();

                        MessageDispatcher.Invoke(type, NetworkReader);

                        if(NetworkReader.Remaining == 0)
                        {
                            End();
                            break;
                        }
                    }
                }

                public static void End()
                {
                    IsOn = false;

                    NetworkAPI.OnProcess -= Process;
                    
                    OnEnd?.Invoke();
                }

                public static event BufferDelegate OnEnd;
            }

            public static class MessageDispatcher
            {
                static Dictionary<Type, BinaryMessageCallbackDelegate> Binary;
                public delegate void BinaryMessageCallbackDelegate(NetworkStream stream);

                static Dictionary<Type, PayloadMessageCallbackDelegate> Payload;
                public delegate void PayloadMessageCallbackDelegate(object target);

                internal static void Configure()
                {
                    Binary = new();
                    Payload = new();
                }

                internal static void Invoke(object payload)
                {
                    var type = payload.GetType();

                    if (Payload.TryGetValue(type, out var callback) == false)
                    {
                        Debug.LogWarning($"Recieved Message with Payload of {type} Has no Handler");
                        return;
                    }

                    callback(payload);
                }
                internal static void Invoke(Type type, NetworkStream stream)
                {
                    if (Binary.TryGetValue(type, out var callback) == false)
                    {
                        Debug.LogWarning($"Recieved Message with Payload of {type} Has no Handler");
                        return;
                    }

                    callback(stream);
                }

                public delegate void MessageHandlerDelegate<TPayload>(ref TPayload payload);
                public static void RegisterHandler<TPayload>(MessageHandlerDelegate<TPayload> handler)
                {
                    var type = typeof(TPayload);

                    if (Binary.ContainsKey(type) || Payload.ContainsKey(type)) throw new Exception($"Client Message Dispatcher Already Contains an Entry for {type}");

                    Binary.Add(type, BinaryCallback);
                    Payload.Add(type, PayloadCallback);

                    void BinaryCallback(NetworkStream stream)
                    {
                        var payload = stream.Read<TPayload>();
                        handler(ref payload);
                    }
                    void PayloadCallback(object target)
                    {
                        var payload = (TPayload)target;
                        handler(ref payload);
                    }
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

                        #region Entity
                        case SpawnEntityRequest instance:
                            return SpawnEntity(ref instance, mode);

                        case TransferEntityPayload instance:
                            return TransferEntity(ref instance, mode);

                        case TakeoverEntityRequest instance:
                            return TakeoverEntity(ref instance, mode);

                        case DestroyEntityPayload instance:
                            return DestroyEntity(ref instance, mode);
                        #endregion

                        #region RPC
                        case BroadcastRpcRequest instance:
                            return InvokeBroadcastRPC(ref instance, mode);

                        case TargetRpcRequest instance:
                            return InvokeTargetRPC(ref instance, mode);

                        case QueryRpcRequest instance:
                            return InvokeQueryRPC(ref instance, mode);

                        case BufferRpcRequest instance:
                            return InvokeBufferRPC(ref instance, mode);
                        #endregion

                        case RprRequest instance:
                            return InvokeRPR(ref instance, mode);

                        #region SyncVar
                        case BroadcastSyncVarRequest instance:
                            return InvokeBroadcastSyncVar(ref instance, mode);

                        case BufferSyncVarRequest instance:
                            return InvokeBufferSyncVar(ref instance, mode);
                        #endregion

                        #region Scenes
                        case LoadScenePayload instance:
                            return LoadScene(ref instance, mode);

                        case UnloadScenePayload instance:
                            return UnloadScene(ref instance, mode);
                        #endregion

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
                        var clients = new NetworkClientInfo[] { new NetworkClientInfo(id, Profile) };
                        var buffer = default(ArraySegment<byte>);
                        var time = TimeResponse.Write(default, request.Time);

                        var response = new RegisterClientResponse(id, OfflineMode.RoomInfo, clients, id, buffer, time);

                        MessageDispatcher.Invoke(response);

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

                        switch (request.Type)
                        {
                            case EntityType.Dynamic:
                                {
                                    var response = SpawnEntityResponse.Write(id, request.Token);

                                    MessageDispatcher.Invoke(response);

                                    return Response.Consume;
                                }

                            case EntityType.SceneObject:
                                {
                                    var command = SpawnEntityCommand.Write(Client.ID, id, request);

                                    MessageDispatcher.Invoke(command);

                                    return Response.Consume;
                                }

                            default:
                                throw new NotImplementedException($"No Case Defined for {request.Type}");
                        }
                    }

                    return Response.Send;
                }

                static Response TransferEntity(ref TransferEntityPayload payload, DeliveryMode mode)
                {
                    MessageDispatcher.Invoke(payload);

                    if (OfflineMode.On) return Response.Consume;

                    return Response.Send;
                }

                static Response TakeoverEntity(ref TakeoverEntityRequest request, DeliveryMode mode)
                {
                    var command = TakeoverEntityCommand.Write(Client.ID, request);

                    MessageDispatcher.Invoke(command);

                    if (OfflineMode.On) return Response.Consume;

                    return Response.Send;
                }

                static Response DestroyEntity(ref DestroyEntityPayload request, DeliveryMode mode)
                {
                    MessageDispatcher.Invoke(request);

                    if (OfflineMode.On)
                    {
                        OfflineMode.EntityIDs.Free(request.ID);

                        return Response.Consume;
                    }

                    return Response.Send;
                }
                #endregion

                #region RPC
                static Response InvokeBroadcastRPC(ref BroadcastRpcRequest request, DeliveryMode mode)
                {
                    if (request.Exception != Client.ID && Groups.Contains(request.Group))
                    {
                        var command = BroadcastRpcCommand.Write(Client.ID, request);

                        MessageDispatcher.Invoke(command);
                    }

                    if (OfflineMode.On) return Response.Consume;

                    return Response.Send;
                }

                static Response InvokeTargetRPC(ref TargetRpcRequest request, DeliveryMode mode)
                {
                    if (request.Target == Client.ID)
                    {
                        var command = TargetRpcCommand.Write(Client.ID, request);

                        MessageDispatcher.Invoke(command);

                        return Response.Consume;
                    }

                    if (OfflineMode.On)
                    {
                        Debug.LogWarning($"Invoking Target RPC on Non-Local Client {request.Target} in Offline Mode!");
                        return Response.Consume;
                    }

                    return Response.Send;
                }

                static Response InvokeQueryRPC(ref QueryRpcRequest request, DeliveryMode mode)
                {
                    if (request.Target == Client.ID)
                    {
                        var command = QueryRpcCommand.Write(Client.ID, request);

                        MessageDispatcher.Invoke(command);

                        return Response.Consume;
                    }

                    if (OfflineMode.On)
                    {
                        Debug.LogWarning($"Invoking Query RPC on Non-Local Client {request.Target} in Offline Mode!");
                        return Response.Consume;
                    }

                    return Response.Send;
                }

                static Response InvokeBufferRPC(ref BufferRpcRequest request, DeliveryMode mode)
                {
                    if (OfflineMode.On)
                    {
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

                        MessageDispatcher.Invoke(response);

                        return Response.Consume;
                    }

                    if (OfflineMode.On)
                    {
                        Debug.LogWarning($"Invoking RPR on Non-Local Client {request.Target} in Offline Mode!");
                        return Response.Consume;
                    }

                    return Response.Send;
                }

                #region Sync Var
                static Response InvokeBroadcastSyncVar(ref BroadcastSyncVarRequest request, DeliveryMode mode)
                {
                    var command = SyncVarCommand.Write(Client.ID, request);

                    MessageDispatcher.Invoke(command);

                    if (OfflineMode.On) return Response.Consume;

                    return Response.Send;
                }

                static Response InvokeBufferSyncVar(ref BufferSyncVarRequest request, DeliveryMode mode)
                {
                    if (OfflineMode.On) return Response.Consume;

                    return Response.Send;
                }
                #endregion

                #region Scenes
                static Response LoadScene(ref LoadScenePayload payload, DeliveryMode mode)
                {
                    MessageDispatcher.Invoke(payload);

                    if (OfflineMode.On) return Response.Consume;

                    return Response.Send;
                }

                static Response UnloadScene(ref UnloadScenePayload payload, DeliveryMode mode)
                {
                    MessageDispatcher.Invoke(payload);

                    if (OfflineMode.On) return Response.Consume;

                    return Response.Send;
                }
                #endregion

                static Response ChangeRoomInfo(ref ChangeRoomInfoPayload payload, DeliveryMode mode)
                {
                    MessageDispatcher.Invoke(payload);

                    if (OfflineMode.On) return Response.Consume;

                    return Response.Send;
                }

                static Response Ping(ref PingRequest request, DeliveryMode mode)
                {
                    if (OfflineMode.On)
                    {
                        var response = new PingResponse(request);

                        MessageDispatcher.Invoke(response);

                        return Response.Consume;
                    }

                    return Response.Send;
                }

                static Response Time(ref TimeRequest request, DeliveryMode mode)
                {
                    if (OfflineMode.On)
                    {
                        var response = new TimeResponse(NetworkAPI.Time.Span, request.Timestamp);

                        MessageDispatcher.Invoke(response);

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
                public static bool IsComplete => Self != null;

                public static string Password { get; internal set; }

                internal static void Configure()
                {
                    MessageDispatcher.RegisterHandler<RegisterClientResponse>(Callback);
                }

                internal static void Request()
                {
                    var request = RegisterClientRequest.Write(Profile, Password);

                    Send(ref request);
                }

                public delegate void callbackDelegate(RegisterClientResponse response);
                public static event callbackDelegate OnCallback;
                static void Callback(ref RegisterClientResponse response)
                {
                    Debug.Log("Client Registered");

                    Client.ReadyCallback(ref response);
                
                    OnCallback?.Invoke(response);
                }

                internal static void Clear()
                {
                    Password = default;
                }
            }

            public static class Groups
            {
                public static HashSet<NetworkGroupID> Collection { get; private set; }

                internal static void Configure()
                {
                    Collection = new HashSet<NetworkGroupID>();
                    Collection.Add(NetworkGroupID.Default);
                }

                public static void Join(params byte[] selection)
                {
                    var ids = Array.ConvertAll(selection, NetworkGroupID.Create);

                    Join(ids);
                }
                public static void Join(params NetworkGroupID[] ids)
                {
                    Add(ids);

                    var payload = new JoinNetworkGroupsPayload(ids);
                    Send(ref payload);
                }

                public static void Leave(params byte[] selection)
                {
                    var ids = Array.ConvertAll(selection, NetworkGroupID.Create);

                    Leave(ids);
                }
                public static void Leave(params NetworkGroupID[] ids)
                {
                    if (ids.Contains(NetworkGroupID.Default))
                        throw new Exception($"Cannot Leave Default Network Group {NetworkGroupID.Default}");

                    Remove(ids);

                    var payload = new LeaveNetworkGroupsPayload(ids);
                    Send(ref payload);
                }

                static void Add(NetworkGroupID[] ids)
                {
                    for (int i = 0; i < ids.Length; i++)
                        Collection.Add(ids[i]);
                }

                public static bool Contains(NetworkGroupID group) => Collection.Contains(group);

                static void Remove(NetworkGroupID[] ids)
                {
                    Collection.RemoveWhere(ids.Contains);
                }

                internal static void Clear()
                {
                    Collection.Clear();
                    Collection.Add(NetworkGroupID.Default);
                }
            }

            public static class Entities
            {
                public static ICollection<NetworkEntity> List => Self?.Entities;

                internal static void Configure()
                {
                    Tokens = new AutoKeyDictionary<EntitySpawnToken, NetworkEntity>(EntitySpawnToken.Min, EntitySpawnToken.Max, EntitySpawnToken.Increment, Constants.IdRecycleLifeTime);

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

                public static NetworkEntity Spawn(GameObject prefab, PersistanceFlags persistance = PersistanceFlags.None, AttributesCollection attributes = null)
                {
                    if (NetworkAPI.Config.SyncedAssets.TryGetIndex(prefab, out var resource) == false)
                        throw new Exception($"Prefab '{prefab}' Not Registerd as a Network Spawnable Object");

                    return Spawn(resource, persistance: persistance, attributes: attributes);
                }

                public static NetworkEntity Spawn(ushort resource, PersistanceFlags persistance = PersistanceFlags.None, AttributesCollection attributes = null)
                {
                    var token = Tokens.Reserve();

                    var instance = Room.Entities.Instantiate(resource);
                    instance.Setup(Self, EntityType.Dynamic, persistance, attributes);

                    Object.DontDestroyOnLoad(instance);

                    Tokens.Assign(token, instance);

                    var request = SpawnEntityRequest.Write(resource, token, persistance, attributes);

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

                    Room.Entities.SpawnLocal(entity, ref payload);
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
                public static void Destroy(INetworkBehaviour behaviour) => Destroy(behaviour.Network.Entity);
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
                static AutoKeyCollection<RprChannelID> channels;

                static Dictionary<RprChannelID, RprPromise> promises;

                internal static void Configure()
                {
                    channels = new AutoKeyCollection<RprChannelID>(RprChannelID.Min, RprChannelID.Max, RprChannelID.Increment, Constants.IdRecycleLifeTime);
                    promises = new Dictionary<RprChannelID, RprPromise>();

                    Client.MessageDispatcher.RegisterHandler<RprCommand>(Fullfil);
                    Client.MessageDispatcher.RegisterHandler<RprResponse>(Fullfil);

                    Room.Clients.OnRemove += ClientRemovedCallback;
                }

                internal static void Clear()
                {
                    channels.Clear();
                    promises.Clear();
                }

                static void ClientRemovedCallback(NetworkClient client)
                {
                    var selection = promises.Values.Where(IsClient).ToArray();
                    bool IsClient(RprPromise promise) => promise.Target == client;

                    foreach (var promise in selection) Fullfil(promise, RemoteResponseType.Disconnect, null);
                }

                #region Fullfil
                static void Fullfil(ref RprCommand command)
                {
                    if (promises.TryGetValue(command.Channel, out var promise) == false)
                    {
                        Debug.LogWarning($"Recieved RPR Command for Channel {command.Channel} But that Channel Doesn't Exist");
                        return;
                    }

                    Fullfil(promise, command.Response, null);
                }

                static void Fullfil(ref RprResponse response)
                {
                    if (promises.TryGetValue(response.Channel, out var promise) == false)
                    {
                        Debug.LogWarning($"Recieved RPR Response for Channel {response.Channel} But that Channel Doesn't Exist");
                        return;
                    }

                    if (Room.Clients.TryGet(response.Sender, out var sender) == false)
                    {
                        Debug.LogWarning($"Recieved RPR Response for Channel {response.Channel} from Unregistered Client {response.Sender}" +
                            $", Will Deliver Response Still");
                    }
                    else if (sender != promise.Target)
                    {
                        Debug.LogWarning($"Recieved RPR Response for Channel {response.Channel} from Client {sender} but That RPR was Targeted towards {promise.Target}");
                        return;
                    }

                    Fullfil(promise, response.Response, response.Raw);
                }

                static void Fullfil(RprPromise promise, RemoteResponseType response, byte[] raw)
                {
                    promise.Fullfil(response, raw);

                    channels.Free(promise.Channel);
                    promises.Remove(promise.Channel);
                }
                #endregion

                internal static RprPromise Promise(NetworkClient target)
                {
                    var channel = channels.Reserve();

                    var promise = new RprPromise(target, channel);

                    promises.Add(channel, promise);

                    return promise;
                }

                #region Respond
                internal static bool Respond(QueryRpcCommand command, RemoteResponseType response)
                {
                    if (response == RemoteResponseType.Success)
                    {
                        Log.Error($"Cannot Respond to RPR with Success Without Providing a Value");
                        return false;
                    }

                    var request = RprRequest.Write(command.Sender, command.Channel, response);
                    return Send(ref request);
                }

                internal static bool Respond(QueryRpcCommand command, object value, Type type)
                {
                    var request = RprRequest.Write(command.Sender, command.Channel, value, type);
                    return Send(ref request);
                }
                #endregion
            }

            public static class System
            {
                public static void Configure()
                {
                    MessageDispatcher.RegisterHandler<SystemMessagePayload>(MessageHandler);
                }

                public delegate void MessageDelegate(SystemMessagePayload payload);
                public static event MessageDelegate OnMessage;
                static void MessageHandler(ref SystemMessagePayload payload)
                {
                    OnMessage?.Invoke(payload);
                }
            }

            public static void Disconnect() => Disconnect(DisconnectCode.Normal);
            public static void Disconnect(DisconnectCode code) => Realtime.Disconnect(code);

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

                Prediction.Clear();
                Register.Clear();
                Groups.Clear();
                Entities.Clear();
                RPR.Clear();

                Room.Clear();
            }
        }
    }
}