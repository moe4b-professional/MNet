﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Server;

using System.IO;
using System.Net;

using SharpHttpRequest = WebSocketSharp.Net.HttpListenerRequest;
using SharpHttpResponse = WebSocketSharp.Net.HttpListenerResponse;
using SharpHttpCode = WebSocketSharp.Net.HttpStatusCode;
using System.Net.Http;

namespace Backend
{
    public class RestAPI
    {
        public HttpServer Server { get; protected set; }

        public RestAPIRouter Router { get; protected set; }

        public void Start()
        {
            Log.Info($"Starting {nameof(RestAPI)}");

            Server.Start();
        }

        void RequestCallback(object sender, HttpRequestEventArgs args)
        {
            var request = args.Request;
            var response = args.Response;

            Log.Info($"{nameof(RestAPI)}: {request.HttpMethod}:{request.Url.AbsolutePath} from {request.UserHostAddress}");

            if (Router.Process(request, response) == false)
                WriteTo(response, SharpHttpCode.NotFound, "Error 404");
        }

        public RestAPI(IPAddress address, int port)
        {
            Log.Info($"Configuring {nameof(RestAPI)} on {address}:{port}");

            Server = new HttpServer(address, port);

            Server.OnGet += RequestCallback;
            Server.OnPost += RequestCallback;
            Server.OnDelete += RequestCallback;
            Server.OnPut += RequestCallback;

            Router = new RestAPIRouter();
        }

        //Static Utility
        #region Write
        public static void WriteTo(SharpHttpResponse response, SharpHttpCode code, string message)
        {
            var data = Encoding.UTF8.GetBytes(message);

            response.StatusCode = (int)code;
            response.ContentEncoding = Encoding.UTF8;
            response.WriteContent(data);
            response.Close();
        }

        public static void WriteTo<TPayload>(SharpHttpResponse response, TPayload payload)
        {
            var raw = NetworkSerializer.Serialize(payload);

            response.StatusCode = (int)HttpStatusCode.OK;
            response.WriteContent(raw);
            response.Close();
        }

        public static ByteArrayContent WriteContent<TPayload>(TPayload payload)
        {
            var binary = NetworkSerializer.Serialize(payload);

            var content = new ByteArrayContent(binary);

            return content;
        }
        #endregion

        #region Read
        public static TPayload Read<TPayload>(SharpHttpRequest request)
            where TPayload : new()
        {
            var binary = Read(request);

            var value = NetworkSerializer.Deserialize<TPayload>(binary);

            return value;
        }

        public static byte[] Read(SharpHttpRequest request)
        {
            using (var memory = new MemoryStream())
            {
                request.InputStream.CopyTo(memory);

                var binary = memory.ToArray();

                return binary;
            }
        }
        public static TPayload Read<TPayload>(HttpResponseMessage response)
            where TPayload : new()
        {
            var binary = response.Content.ReadAsByteArrayAsync().Result;

            var result = NetworkSerializer.Deserialize<TPayload>(binary);

            return result;
        }
        #endregion
    }

    public class RestAPIRouter
    {
        public Dictionary<string, ProcessDelegate> Dictionary { get; protected set; }

        public delegate void ProcessDelegate(SharpHttpRequest request, SharpHttpResponse response);

        public virtual bool Process(SharpHttpRequest request, SharpHttpResponse response)
        {
            if (Dictionary.TryGetValue(request.RawUrl, out var callback) == false) return false;

            callback(request, response);
            return true;
        }

        public virtual void Register(string url, ProcessDelegate callback)
        {
            if (Dictionary.ContainsKey(url))
                throw new ArgumentException($"URL {url} Already Registerd For Rest API Routing");

            Dictionary.Add(url, callback);
        }

        public RestAPIRouter()
        {
            Dictionary = new Dictionary<string, ProcessDelegate>();
        }
    }
}