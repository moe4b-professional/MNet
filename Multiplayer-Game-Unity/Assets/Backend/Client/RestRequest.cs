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

using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Networking;

using Game.Shared;

namespace Game
{
	public static class RestRequest
	{
        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {
            List = new List<Element>();
            Queue = new Queue<Element>();

            var loop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < loop.subSystemList.Length; ++i)
                if (loop.subSystemList[i].type == typeof(Update))
                    loop.subSystemList[i].updateDelegate += Update;

            PlayerLoop.SetPlayerLoop(loop);
        }

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

            if(request.isDone == false)
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
            var url = JoinURL(address, path);

            var request = new UnityWebRequest(url, method);

            request.downloadHandler = new DownloadHandlerBuffer();

            Register(request, callback, enqueue);
        }

        public static void POST(string address, string path, NetworkMessage message, CallbackDelegate callback, bool enqueue)
        {

        }

        public static void Send(UnityWebRequest request, CallbackDelegate callback, bool enqueue)
        {

        }

        public static string JoinURL(string address, string path)
        {
            var url = "http://" + address + ":" + Constants.RestAPI.Port + path;

            return url;
        }

        public delegate void CallbackDelegate(NetworkMessage message, RestError error);
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