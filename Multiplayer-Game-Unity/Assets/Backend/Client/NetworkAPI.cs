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
using UnityEngine.PlayerLoop;
using UnityEngine.LowLevel;

using WebSocketSharp;
using WebSocketSharp.Net;

using System.Collections.Concurrent;

namespace Backend
{
	public static class NetworkAPI
	{
        public static string Address { get; private set; }

        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < loop.subSystemList.Length; ++i)
                if (loop.subSystemList[i].type == typeof(Update))
                    loop.subSystemList[i].updateDelegate += Update;

            PlayerLoop.SetPlayerLoop(loop);
        }

        public static void Configure(string address)
        {
            NetworkAPI.Address = address;

            Log.Output = LogOutput;

            Client.Configure();
            Room.Configure();
        }

        static void LogOutput(object target, Log.Level level)
        {
            switch (level)
            {
                case Log.Level.Info:
                    Debug.Log(target);
                    break;

                case Log.Level.Warning:
                    Debug.LogWarning(target);
                    break;

                case Log.Level.Error:
                    Debug.LogError(target);
                    break;

                default:
                    Debug.LogWarning($"No Logging Case Made For Log Level {level}");
                    Debug.Log(target);
                    break;
            }
        }

        public static event Action OnUpdate;
        public static void Update()
        {
            OnUpdate?.Invoke();
        }

        public static class RestAPI
        {
            public static string Address => NetworkAPI.Address + ":" + Constants.RestAPI.Port;

            public static class Request
            {
                public static List<Element> List { get; private set; }
                public static Queue<Element> Queue { get; private set; }
                public class Element
                {
                    public UnityWebRequest Request { get; protected set; }

                    public CallbackDelegate Callback { get; protected set; }

                    public Element(UnityWebRequest request, CallbackDelegate callback)
                    {
                        this.Request = request;

                        this.Callback = callback;
                    }
                }

                static bool IsDone(Element element) => element.Request.isDone;

                public static Element Register(UnityWebRequest request, CallbackDelegate callback, bool inqueue)
                {
                    var element = new Element(request, callback);

                    if (inqueue)
                        Queue.Enqueue(element);
                    else
                        List.Add(element);

                    request.SendWebRequest();

                    return element;
                }

                static void Update()
                {
                    for (int i = 0; i < List.Count; i++)
                        if (IsDone(List[i]))
                            Process(List[i]);

                    List.RemoveAll(IsDone);

                    while (true)
                    {
                        if (Queue.Count == 0) break;

                        var element = Queue.Peek();

                        if (IsDone(element))
                            Process(element);
                        else
                            break;

                        Queue.Dequeue();
                    }
                }

                static void Process(Element element)
                {
                    var request = element.Request;

                    if (request.isDone == false)
                    {
                        Debug.LogWarning($"{request.url} Rest request still isn't done, cannot process in this state, ignoring");
                        return;
                    }

                    var callback = element.Callback;

                    if (request.isHttpError || request.isNetworkError)
                    {
                        var error = new RestError(request);

                        callback(null, error);
                    }
                    else
                    {
                        var message = NetworkMessage.Read(request.downloadHandler.data);

                        callback(message, null);
                    }
                }

                public static void GET(string address, string path, CallbackDelegate callback, bool enqueue)
                {
                    var downloader = new DownloadHandlerBuffer();

                    Send(address, path, "GET", null, downloader, callback, enqueue);
                }

                public static void POST(string address, string path, NetworkMessage message, CallbackDelegate callback, bool enqueue)
                {
                    var data = NetworkSerializer.Serialize(message);

                    var uploader = new UploadHandlerRaw(data);

                    var downloader = new DownloadHandlerBuffer();

                    Send(address, path, "POST", uploader, downloader, callback, enqueue);
                }

                public static void Send(string address, string path, string method, UploadHandler uploader, DownloadHandler downloader, CallbackDelegate callback, bool enqueue)
                {
                    var url = "http://" + address + path;

                    var request = new UnityWebRequest(url, method, downloader, uploader);

                    Register(request, callback, enqueue);
                }

                public delegate void CallbackDelegate(NetworkMessage message, RestError error);

                static Request()
                {
                    List = new List<Element>();
                    Queue = new Queue<Element>();

                    NetworkAPI.OnUpdate += Update;
                }
            }

            public static class Lobby
            {
                #region List
                public static void Info()
                {
                    Request.GET(Address, Constants.RestAPI.Requests.Lobby.Info, InfoCallback, false);
                }

                public delegate void InfoDelegate(LobbyInfo lobby);
                public static event InfoDelegate OnInfo;
                static void InfoCallback(NetworkMessage message, RestError error)
                {
                    if (error == null)
                    {
                        var info = message.Read<LobbyInfo>();

                        OnInfo?.Invoke(info);
                    }
                    else
                        ProcessErorr(error);
                }
                #endregion
            }

            public static class Room
            {
                #region Create
                public static void Create(string name, ushort capacity)
                {
                    var request = new CreateRoomRequest(name, capacity);

                    Create(request);
                }
                public static void Create(string name, ushort capacity, AttributesCollection attributes)
                {
                    var request = new CreateRoomRequest(name, capacity, attributes);

                    Create(request);
                }
                public static void Create(CreateRoomRequest request)
                {
                    var message = NetworkMessage.Write(request);

                    Request.POST(Address, Constants.RestAPI.Requests.Room.Create, message, CreateCallback, false);
                }

                public delegate void CreatedDelegate(RoomBasicInfo room);
                public static event CreatedDelegate OnCreated;
                static void CreateCallback(NetworkMessage message, RestError error)
                {
                    if (error == null)
                    {
                        var info = message.Read<RoomBasicInfo>();

                        OnCreated?.Invoke(info);
                    }
                    else
                        ProcessErorr(error);
                }
                #endregion
            }

            static void ProcessErorr(RestError error)
            {
                Debug.LogError("Rest Error: " + error.Message);
            }
        }

        public static class WebSocketAPI
        {
            public static string Address => NetworkAPI.Address + ":" + Constants.WebSocketAPI.Port;
            
            public static WebSocket Client { get; private set; }
            public static bool IsConnected => Client == null ? false : Client.ReadyState == WebSocketState.Open;

            public static ConcurrentQueue<Action> ActionQueue { get; private set; }

            public static void Connect(string path)
            {
                if (IsConnected) Disconnect();

                var url = "ws://" + Address + path;

                ActionQueue = new ConcurrentQueue<Action>();

                Client = new WebSocket(url);

                Client.OnOpen += ConnectCallback;
                Client.OnMessage += MessageCallback;
                Client.OnClose += DisconnectCallback;
                Client.OnError += ErrorCallback;

                Client.ConnectAsync();
            }

            public static void Send(NetworkMessage message)
            {
                var binary = NetworkSerializer.Serialize(message);

                Client.SendAsync(binary, null);
            }

            static void Update()
            {
                while (ActionQueue.TryDequeue(out var callback))
                    callback();
            }

            #region Callbacks
            public delegate void ConnectDelegate();
            public static event ConnectDelegate OnConnect;
            static void ConnectCallback(object sender, EventArgs args)
            {
                ActionQueue.Enqueue(Invoke);

                void Invoke() => OnConnect?.Invoke();
            }

            public delegate void MessageDelegate(NetworkMessage message);
            public static event MessageDelegate OnMessage;
            static void MessageCallback(object sender, MessageEventArgs args)
            {
                var message = NetworkMessage.Read(args.RawData);

                ActionQueue.Enqueue(Invoke);

                void Invoke() => OnMessage?.Invoke(message);
            }

            public delegate void CloseDelegate(CloseStatusCode code, string reason);
            public static event CloseDelegate OnDisconnect;
            static void DisconnectCallback(object sender, CloseEventArgs args)
            {
                var code = (CloseStatusCode)args.Code;
                var reason = args.Reason;

                ActionQueue = new ConcurrentQueue<Action>();
                ActionQueue.Enqueue(Invoke);

                void Invoke() => OnDisconnect?.Invoke(code, reason);
            }

            public delegate void ErrorDelegate(Exception exception, string message);
            public static event ErrorDelegate OnError;
            static void ErrorCallback(object sender, WebSocketSharp.ErrorEventArgs args)
            {
                ActionQueue.Enqueue(Invoke);

                void Invoke() => OnError?.Invoke(args.Exception, args.Message);
            }
            #endregion
            
            static void ApplicationQuitCallback()
            {
                if (IsConnected) Disconnect(CloseStatusCode.Normal);
            }

            public static void Disconnect() => Disconnect(CloseStatusCode.Normal);
            public static void Disconnect(CloseStatusCode code)
            {
                if (IsConnected == false) return;

                Client.CloseAsync(code);
            }

            static WebSocketAPI()
            {
                ActionQueue = new ConcurrentQueue<Action>();

                NetworkAPI.OnUpdate += Update;

                Application.quitting += ApplicationQuitCallback;
            }
        }

        public static class Client
        {
            public static NetworkClientProfile Profile { get; private set; }

            public static NetworkClientID ID => Instance.ID;

            public static bool IsConnected => WebSocketAPI.IsConnected;

            public static bool IsReady { get; private set; }

            public static NetworkClient Instance { get; private set; }
            public static void Set(NetworkClientID ID) => Instance = new NetworkClient(ID, Profile);

            public static void Configure()
            {
                WebSocketAPI.OnConnect += ConnectCallback;
                WebSocketAPI.OnMessage += MessageCallback;
                WebSocketAPI.OnDisconnect += DisconnectedCallback;

                Room.OnSpawnEntity += SpawnEntityCallback;

                IsReady = false;
            }

            public static void Send(NetworkMessage message) => WebSocketAPI.Send(message);

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
                Profile = new NetworkClientProfile("Moe4B");

                var request = new RegisterClientRequest(Profile);

                var message = NetworkMessage.Write(request);

                Send(message);
            }

            public static event Action OnRegister;
            static void RegisterCallback(RegisterClientResponse response)
            {
                Set(response.ID);

                if (AutoReady) RequestReady();

                OnRegister?.Invoke();
            }
            #endregion

            #region Ready
            public static bool AutoReady { get; set; } = true;

            public static void RequestReady()
            {
                var request = new ReadyClientRequest();

                var message = NetworkMessage.Write(request);

                Send(message);
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
                var request = new SpawnEntityRequest(resource, attributes);

                var message = NetworkMessage.Write(request);

                Send(message);
            }

            public delegate void SpawnEntityDelegate(NetworkEntity entity, string resource, AttributesCollection attributes);
            public static event SpawnEntityDelegate OnSpawnEntity;
            static void SpawnEntityCallback(NetworkEntity entity, NetworkClient owner, string resource, AttributesCollection attributes)
            {
                if (owner?.ID == Client.ID) OnSpawnEntity?.Invoke(entity, resource, attributes);
            }
            #endregion

            public delegate void DisconnectDelegate(CloseStatusCode code, string reason);
            public static event DisconnectDelegate OnDisconnect;
            static void DisconnectedCallback(CloseStatusCode code, string reason)
            {
                Debug.Log("Client Disconnected");

                IsReady = false;

                OnDisconnect?.Invoke(code, reason);
            }
        }

        public static class Room
        {
            public static Dictionary<NetworkClientID, NetworkClient> Clients { get; private set; }

            public static Dictionary<NetworkEntityID, NetworkEntity> Entities { get; private set; }

            public static void Configure()
            {
                Client.OnMessage += ClientMessageCallback;
                Client.OnReady += ClientReadyCallback;
            }

            public static void Join(RoomBasicInfo info) => Join(info.ID);
            public static void Join(ushort id)
            {
                Clients = new Dictionary<NetworkClientID, NetworkClient>();

                Entities = new Dictionary<NetworkEntityID, NetworkEntity>();

                WebSocketAPI.Connect("/" + id);
            }

            static void ClientReadyCallback(ReadyClientResponse response)
            {
                RegisterClientsInternal(response.Clients);

                ApplyMessageBuffer(response.Buffer);
            }

            static void ClientMessageCallback(NetworkMessage message)
            {
                if(Client.IsReady)
                {
                    if (message.Is<RpcCommand>())
                    {
                        var command = message.Read<RpcCommand>();

                        InvokeRpc(command);
                    }
                    else if (message.Is<SpawnEntityCommand>())
                    {
                        var command = message.Read<SpawnEntityCommand>();

                        SpawnEntity(command);
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
            }

            static void RegisterClientsInternal(IList<NetworkClientInfo> list)
            {
                for (int i = 0; i < list.Count; i++)
                    RegisterClientInternal(list[i]);
            }
            static NetworkClient RegisterClientInternal(NetworkClientInfo info) => RegisterClientInternal(info.ID, info.Profile);
            static NetworkClient RegisterClientInternal(NetworkClientID id, NetworkClientProfile profile)
            {
                var client = new NetworkClient(id, profile);

                Clients.Add(client.ID, client);

                return client;
            }

            static void ApplyMessageBuffer(IList<NetworkMessage> list)
            {
                for (int i = 0; i < list.Count; i++)
                    ClientMessageCallback(list[i]);
            }

            public delegate void ClientConnectedDelegate(NetworkClient client);
            public static event ClientConnectedDelegate OnClientConnected;
            static void ClientConnected(ClientConnectedPayload payload)
            {
                if(Clients.ContainsKey(payload.ID))
                {
                    Debug.Log($"Connecting Client {payload.ID} Already Registered With Room");
                    return;
                }

                var client = RegisterClientInternal(payload.ID, payload.Profile);

                OnClientConnected?.Invoke(client);

                Debug.Log($"Client {client.ID} Connected to Room");
            }

            static void InvokeRpc(RpcCommand command)
            {
                if (Entities.TryGetValue(command.Entity, out var target))
                    target.InvokeRpc(command);
                else
                    Debug.LogWarning($"No {nameof(NetworkEntity)} found with ID {command.Entity}");
            }

            public delegate void SpawnEntityDelegate(NetworkEntity entity, NetworkClient owner, string resource, AttributesCollection attributes);
            public static event SpawnEntityDelegate OnSpawnEntity;
            static void SpawnEntity(SpawnEntityCommand command)
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
                    Object.Destroy(instance);
                    return;
                }

                if (Clients.TryGetValue(command.Owner, out var owner))
                    owner.Entities.Add(entity);
                else
                    Debug.LogWarning($"Spawned Entity {entity.name} Has No Client Owner");

                entity.Configure(owner, command.Entity, command.Attributes);
                Entities.Add(entity.ID, entity);

                OnSpawnEntity?.Invoke(entity, owner, command.Resource, command.Attributes);
            }

            public delegate void ClientDisconnectedDelegate(NetworkClientID id, NetworkClientProfile info);
            public static event ClientDisconnectedDelegate OnClientDisconnected;
            static void ClientDisconnected(ClientDisconnectPayload payload)
            {
                Debug.Log($"Client {payload.ID} Disconnected to Room");

                if (Clients.TryGetValue(payload.ID, out var client))
                    RemoveClient(client);
                else
                    Debug.Log($"Disconnecting Client {payload.ID} Not Found In Room");

                OnClientDisconnected?.Invoke(payload.ID, payload.Profile);
            }

            static void RemoveClient(NetworkClient client)
            {
                foreach (var entity in client.Entities)
                {
                    Entities.Remove(entity.ID);

                    Object.Destroy(entity.gameObject);
                }

                Clients.Remove(client.ID);
            }

            public static void Leave()
            {
                WebSocketAPI.Disconnect();
            }
        }
    }
}