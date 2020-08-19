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
	public static class NetworkClient
	{
        public static string Address { get; private set; }

        public static string ID { get; private set; }

        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {
            ID = string.Empty;

            var loop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < loop.subSystemList.Length; ++i)
                if (loop.subSystemList[i].type == typeof(Update))
                    loop.subSystemList[i].updateDelegate += Update;

            PlayerLoop.SetPlayerLoop(loop);
        }

        public static void Configure(string address)
        {
            NetworkClient.Address = address;
        }

        public static event Action OnUpdate;
        public static void Update()
        {
            OnUpdate?.Invoke();
        }

        public static class RestAPI
        {
            public static string Address => NetworkClient.Address + ":" + Constants.RestAPI.Port;

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

                    NetworkClient.OnUpdate += Update;
                }
            }

            public static class Room
            {
                #region List
                public static void List()
                {
                    Request.GET(Address, Constants.RestAPI.Requests.Room.List, ListCallback, false);
                }

                public delegate void ListDelegate(RoomInfo[] rooms);
                public static event ListDelegate OnList;
                static void ListCallback(NetworkMessage message, RestError error)
                {
                    if (error == null)
                    {
                        var payload = message.Read<RoomListInfoPayload>();

                        var list = payload.List;

                        OnList?.Invoke(list);
                    }
                    else
                        ProcessErorr(error);
                }
                #endregion

                #region Create
                public static void Create(string name, short capacity) => Create(new CreateRoomPayload(name, capacity));
                public static void Create(CreateRoomPayload payload)
                {
                    var message = payload.ToMessage();

                    Request.POST(Address, Constants.RestAPI.Requests.Room.Create, message, CreateCallback, false);
                }

                public delegate void CreatedDelegate(RoomInfo room);
                public static event CreatedDelegate OnCreated;
                static void CreateCallback(NetworkMessage message, RestError error)
                {
                    if (error == null)
                    {
                        var payload = message.Read<RoomInfoPayload>();

                        var list = payload.Info;

                        OnCreated?.Invoke(list);
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
            public static string Address => NetworkClient.Address + ":" + Constants.WebSocketAPI.Port;
            
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

                NetworkClient.OnUpdate += Update;

                Application.quitting += ApplicationQuitCallback;
            }
        }

        public static class Room
        {
            public static Dictionary<string, NetworkIdentity> Identities { get; private set; }

            public static void Join(ushort id)
            {
                WebSocketAPI.Connect("/" + id);
            }

            #region Utility
            public static void Send(NetworkMessage message) => WebSocketAPI.Send(message);

            public static void RequestSpawn(string resource)
            {
                var message = new SpawnObjectRequestPayload(resource).ToMessage();

                Send(message);
            }
            #endregion

            #region Callbacks
            static void ConnectedCallback()
            {
                Debug.Log("Connected to Room");

                RequestSpawn("Player");
            }

            static void MessageCallback(NetworkMessage message)
            {
                if (message.Is<SpawnObjectCommandPayload>())
                {
                    var payload = message.Read<SpawnObjectCommandPayload>();

                    SpawnCommand(payload);
                }

                if(message.Is<RpcPayload>())
                {
                    var payload = message.Read<RpcPayload>();

                    InvokeRpc(payload);
                }
            }
            #endregion

            static void InvokeRpc(RpcPayload payload)
            {
                if (Identities.TryGetValue(payload.Identity, out var identity))
                    identity.InvokeRpc(payload);
                else
                    Debug.LogWarning($"No {nameof(NetworkIdentity)} found with ID {payload.Identity}");
            }

            static void SpawnCommand(SpawnObjectCommandPayload payload)
            {
                Debug.Log($"Spawn {payload.Resource} with ID: {payload.ID}");

                var prefab = Resources.Load<GameObject>(payload.Resource);

                if(prefab == null)
                {
                    Debug.LogError($"No Resource {payload.Resource} Found to Spawn");
                    return;
                }

                var instance = Object.Instantiate(prefab);

                var identity = instance.GetComponent<NetworkIdentity>();

                if(identity == null)
                {
                    Debug.LogError($"No {nameof(NetworkIdentity)} Found on Resource {payload.Resource}");
                    return;
                }

                Identities.Add(payload.ID, identity);

                identity.Spawn(payload.ID);
            }

            static Room()
            {
                Identities = new Dictionary<string, NetworkIdentity>();

                WebSocketAPI.OnConnect += ConnectedCallback;
                WebSocketAPI.OnMessage += MessageCallback;
            }
        }
	}

    public struct ClientID
    {
        [SerializeField]
        private string value;
        public string Value { get { return value; } }

        public ClientID(string id)
        {
            this.value = id;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(ClientID))
            {
                var target = (ClientID)obj;

                return target.value == this.value;
            }

            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(ClientID a, ClientID b) => a.Equals(b);
        public static bool operator !=(ClientID a, ClientID b) => !a.Equals(b);

        public static ClientID Empty { get; private set; } = new ClientID(string.Empty);
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