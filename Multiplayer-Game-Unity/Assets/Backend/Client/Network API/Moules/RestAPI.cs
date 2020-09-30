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

namespace Backend
{
	public static partial class NetworkAPI
	{
        public static class RestAPI
        {
            public static string Address => NetworkAPI.Address + ":" + Constants.GameServer.Rest.Port;

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

                callback(request);
            }

            public static void GET(string address, string path, CallbackDelegate callback, bool enqueue)
            {
                var downloader = new DownloadHandlerBuffer();

                Send(address, path, "GET", null, downloader, callback, enqueue);
            }
            public static void PUT(string address, string path, NetworkMessage message, CallbackDelegate callback, bool enqueue)
            {
                var data = NetworkSerializer.Serialize(message);

                var uploader = new UploadHandlerRaw(data);
                var downloader = new DownloadHandlerBuffer();

                Send(address, path, "PUT", uploader, downloader, callback, enqueue);
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

            public static void Parse<T>(UnityWebRequest request, out T payload, out RestError error)
                where T : new()
            {
                if (request.isHttpError || request.isNetworkError)
                {
                    payload = default;
                    error = new RestError(request);
                    return;
                }

                try
                {
                    var raw = request.downloadHandler.data;

                    payload = NetworkSerializer.Deserialize<T>(raw);
                    error = null;

                    return;
                }
                catch (Exception)
                {
                    payload = default;
                    error = new RestError(0, $"Error Parsing {typeof(T).Name} From Server Response");
                }
            }

            public delegate void CallbackDelegate(UnityWebRequest request);

            public static class Lobby
            {
                #region List
                public static void Info()
                {
                    GET(Address, Constants.GameServer.Rest.Requests.Lobby.Info, InfoCallback, false);
                }

                public delegate void InfoDelegate(LobbyInfo lobby, RestError error);
                public static event InfoDelegate OnInfo;
                static void InfoCallback(UnityWebRequest request)
                {
                    Parse(request, out LobbyInfo info, out var error);

                    OnInfo(info, error);
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

                    POST(Address, Constants.GameServer.Rest.Requests.Room.Create, message, CreateCallback, false);
                }

                public delegate void CreatedDelegate(RoomBasicInfo room, RestError error);
                public static event CreatedDelegate OnCreated;
                static void CreateCallback(UnityWebRequest request)
                {
                    Parse(request, out RoomBasicInfo info, out var error);

                    OnCreated(info, error);
                }
                #endregion
            }

            static RestAPI()
            {
                List = new List<Element>();
                Queue = new Queue<Element>();

                NetworkAPI.OnUpdate += Update;
            }
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

        public override string ToString() => $"REST Error: {Message}";
    }
}