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
	public static class NetworkAPI
	{
        public static string Address { get; private set; }

        public static class Client
        {
            public static NetworkClientInfo Info { get; private set; }

            public static NetworkClient Instance { get; private set; }

            public static void Register(NetworkClientID ID)
            {
                Instance = new NetworkClient(ID, Info);
            }
        }

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
                public static void Create(string name, ushort capacity) => Create(new CreateRoomRequest(name, capacity));
                public static void Create(CreateRoomRequest request)
                {
                    var message = NetworkMessage.Write(request);

                    Request.POST(Address, Constants.RestAPI.Requests.Room.Create, message, CreateCallback, false);
                }

                public delegate void CreatedDelegate(RoomInfo room);
                public static event CreatedDelegate OnCreated;
                static void CreateCallback(NetworkMessage message, RestError error)
                {
                    if (error == null)
                    {
                        var info = message.Read<RoomInfo>();

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
            public static bool IsConnected => Client == null ? false : Client.IsAlive;

            public static ConcurrentQueue<ActionCallback> ActionQueue { get; private set; }
            public delegate void ActionCallback();

            public static void Connect(string path)
            {
                if (IsConnected) Disconnect();

                var url = "ws://" + Address + path;

                Client = new WebSocket(url);

                Client.OnOpen += ConnectCallback;
                Client.OnMessage += MessageCallback;
                Client.OnClose += CloseCallback;
                Client.OnError += ErrorCallback;

                Client.ConnectAsync();
            }

            public static void Send(NetworkMessage message)
            {
                var binary = NetworkSerializer.Serialize(message);

                Client.Send(binary);
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
            public static event CloseDelegate OnClose;
            static void CloseCallback(object sender, CloseEventArgs args)
            {
                var code = (CloseStatusCode)args.Code;
                var reason = args.Reason;

                ActionQueue.Enqueue(Invoke);

                void Invoke() => OnClose?.Invoke(code, reason);
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
                if (IsConnected) Client.CloseAsync(code);
            }

            static WebSocketAPI()
            {
                ActionQueue = new ConcurrentQueue<ActionCallback>();

                NetworkAPI.OnUpdate += Update;

                Application.quitting += ApplicationQuitCallback;
            }
        }

        public static NetworkRoom Room { get; private set; }
    }

    public class NetworkRoom
    {
        public static Dictionary<NetworkClientID, NetworkClient> Clients { get; protected set; }

        public static Dictionary<NetworkEntityID, NetworkEntity> Entities { get; private set; }

        public static void Join(ushort id)
        {
            NetworkAPI.WebSocketAPI.Connect("/" + id);
        }

        #region Utility
        public void Send(NetworkMessage message) => NetworkAPI.WebSocketAPI.Send(message);

        public void RequestReady()
        {
            var request = new ReadyClientRequest(NetworkAPI.Client.Info);

            var message = NetworkMessage.Write(request);

            Send(message);
        }

        public void RequestSpawn(string resource)
        {
            var request = new SpawnEntityRequest(resource);

            var message = NetworkMessage.Write(request);

            Send(message);
        }
        #endregion

        void ConnectCallback()
        {
            Debug.Log("Connected to Room");

            RequestReady();
        }

        #region Messages
        void MessageCallback(NetworkMessage message)
        {
            if (message.Is<ReadyClientResponse>())
            {
                var response = message.Read<ReadyClientResponse>();

                SetReady(response);
            }
            else if (message.Is<SpawnEntityCommand>())
            {
                var command = message.Read<SpawnEntityCommand>();

                SpawnEntity(command);
            }
            else if (message.Is<RpcCommand>())
            {
                var command = message.Read<RpcCommand>();

                InvokeRpc(command);
            }
            else if(message.Is<ClientConnectedPayload>())
            {
                var payload = message.Read<ClientConnectedPayload>();

                ClientConnected(payload);
            }
            else if(message.Is<ClientDisconnectPayload>())
            {
                var payload = message.Read<ClientDisconnectPayload>();

                ClientDisconnected(payload);
            }
        }

        void ClientConnected(ClientConnectedPayload payload)
        {

        }

        void SetReady(ReadyClientResponse response)
        {
            NetworkAPI.Client.Register(response.ClientID);

            for (int i = 0; i < response.MessageBuffer.Count; i++)
                MessageCallback(response.MessageBuffer[i]);

            RequestSpawn("Player");
        }

        void InvokeRpc(RpcCommand command)
        {
            if (Entities.TryGetValue(command.Entity, out var target))
                target.InvokeRpc(command);
            else
                Debug.LogWarning($"No {nameof(NetworkEntity)} found with ID {command.Entity}");
        }

        void SpawnEntity(SpawnEntityCommand command)
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

            Entities.Add(command.Entity, entity);

            entity.Spawn(command.Owner, command.Entity);
        }

        void ClientDisconnected(ClientDisconnectPayload payload)
        {

        }
        #endregion

        public NetworkRoom()
        {
            Entities = new Dictionary<NetworkEntityID, NetworkEntity>();

            NetworkAPI.WebSocketAPI.OnConnect += ConnectCallback;
            NetworkAPI.WebSocketAPI.OnMessage += MessageCallback;
        }
    }

    public class RestError
    {
        public long Code { get; protected set; }

        public string Message { get; protected set; }

        public RestError(long code, string message)
        {
            this.Code = code;
            this.Message = message;
        }

        public RestError(UnityWebRequest request) : this(request.responseCode, request.error) { }
    }
}