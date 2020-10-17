﻿using System;
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
using System.Net;

namespace MNet
{
    public abstract class RestAPI
    {
        public ushort Port { get; protected set; }
        public static RestScheme Scheme { get; protected set; }

        public List<Element> List { get; private set; }
        public Queue<Element> Queue { get; private set; }
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

        public Element Register(UnityWebRequest request, CallbackDelegate callback, bool inqueue)
        {
            var element = new Element(request, callback);

            if (inqueue)
                Queue.Enqueue(element);
            else
                List.Add(element);

            request.SendWebRequest();

            return element;
        }

        void Update()
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

        void Process(Element element)
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

        protected void Send(string ip, string path, string method, UploadHandler uploader, DownloadHandler downloader, CallbackDelegate callback, bool enqueue)
        {
            var url = $"{Scheme}://{ip}:{Port}{path}";

            var request = new UnityWebRequest(url, method, downloader, uploader);

            Register(request, callback, enqueue);
        }

        public delegate void CallbackDelegate(UnityWebRequest request);

        public RestAPI(ushort port)
        {
            this.Port = port;

            List = new List<Element>();
            Queue = new Queue<Element>();

            MNetAPI.OnUpdate += Update;
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
    }

    public class GenericRestAPI : RestAPI
    {
        public void GET(string ip, string path, CallbackDelegate callback, bool enqueue)
        {
            var downloader = new DownloadHandlerBuffer();

            Send(ip, path, "GET", null, downloader, callback, enqueue);
        }
        public void PUT<T>(string ip, string path, T payload, CallbackDelegate callback, bool enqueue)
        {
            var data = NetworkSerializer.Serialize(payload);

            var uploader = new UploadHandlerRaw(data);
            var downloader = new DownloadHandlerBuffer();

            Send(ip, path, "PUT", uploader, downloader, callback, enqueue);
        }
        public void POST<T>(string ip, string path, T payload, CallbackDelegate callback, bool enqueue)
        {
            var data = NetworkSerializer.Serialize(payload);

            var uploader = new UploadHandlerRaw(data);
            var downloader = new DownloadHandlerBuffer();

            Send(ip, path, "POST", uploader, downloader, callback, enqueue);
        }

        public GenericRestAPI(ushort port) : base(port)
        {

        }
    }

    public class DirectedRestAPI : RestAPI
    {
        public string IP { get; protected set; }
        public void SetIP(string value)
        {
            this.IP = value;
        }

        public void GET(string path, CallbackDelegate callback, bool enqueue)
        {
            var downloader = new DownloadHandlerBuffer();

            Send(IP, path, "GET", null, downloader, callback, enqueue);
        }
        public void PUT<T>(string path, T payload, CallbackDelegate callback, bool enqueue)
        {
            var data = NetworkSerializer.Serialize(payload);

            var uploader = new UploadHandlerRaw(data);
            var downloader = new DownloadHandlerBuffer();

            Send(IP, path, "PUT", uploader, downloader, callback, enqueue);
        }
        public void POST<T>(string path, T payload, CallbackDelegate callback, bool enqueue)
        {
            var data = NetworkSerializer.Serialize(payload);

            var uploader = new UploadHandlerRaw(data);
            var downloader = new DownloadHandlerBuffer();

            Send(IP, path, "POST", uploader, downloader, callback, enqueue);
        }

        public DirectedRestAPI(ushort port) : base(port)
        {

        }
        public DirectedRestAPI(string ip, ushort port) : this(port)
        {
            SetIP(ip);
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