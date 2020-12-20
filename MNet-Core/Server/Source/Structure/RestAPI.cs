using System;
using System.Linq;
using System.Text;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using System.Threading.Tasks;

using System.IO;

using System.Net;
using System.Net.Http;

using WebSocketSharp;
using WebSocketSharp.Server;

using RestRequest = WebSocketSharp.Net.HttpListenerRequest;
using RestResponse = WebSocketSharp.Net.HttpListenerResponse;

namespace MNet
{
    public static class RestServerAPI
    {
        public static HttpServer Server { get; private set; }

        public static class Router
        {
            public static Dictionary<string, ProcessDelegate> Dictionary { get; private set; }

            public delegate void ProcessDelegate(RestRequest request, RestResponse response);

            public static bool Process(RestRequest request, RestResponse response)
            {
                if (Dictionary.TryGetValue(request.RawUrl, out var callback) == false) return false;

                callback(request, response);
                return true;
            }

            public static void Register(string url, ProcessDelegate callback)
            {
                if (Dictionary.ContainsKey(url))
                    throw new ArgumentException($"URL {url} Already Registerd For Rest API Routing");

                Dictionary.Add(url, callback);
            }

            static Router()
            {
                Dictionary = new Dictionary<string, ProcessDelegate>();
            }
        }

        public static void Configure(ushort port)
        {
            Log.Info($"Configuring Rest API on Port:{port}");

            Server = new HttpServer(IPAddress.Any, port);

            Server.OnGet += RequestCallback;
            Server.OnPost += RequestCallback;
            Server.OnDelete += RequestCallback;
            Server.OnPut += RequestCallback;
        }

        public static void Start()
        {
            Log.Info($"Starting Rest API");

            Server.Start();
        }

        static void RequestCallback(object sender, HttpRequestEventArgs args)
        {
            var request = args.Request;
            var response = args.Response;

            Log.Info($"Rest API: {request.HttpMethod}:{request.Url.AbsolutePath} from {request.UserHostAddress}");

            if (Router.Process(request, response) == false) Write(response, RestStatusCode.NotFound, "Error 404");
        }

        //Static Utility

        #region Write
        public static void Write(RestResponse response, RestStatusCode code) => Write(response, code, code.ToString());
        public static void Write(RestResponse response, RestStatusCode code, string message)
        {
            var data = Encoding.UTF8.GetBytes(message);

            response.StatusCode = (int)code;
            response.StatusDescription = message;

            response.ContentEncoding = Encoding.UTF8;
            response.WriteContent(data);

            response.Close();
        }

        public static void Write<TPayload>(RestResponse response, TPayload payload)
        {
            var raw = NetworkSerializer.Serialize(payload);

            response.StatusCode = (int)HttpStatusCode.OK;
            response.WriteContent(raw);
            response.Close();
        }
        #endregion

        #region Read
        public static void Read<TPayload>(RestRequest request, out TPayload payload) => payload = Read<TPayload>(request);
        public static TPayload Read<TPayload>(RestRequest request)
        {
            var binary = Read(request);

            var value = NetworkSerializer.Deserialize<TPayload>(binary);

            return value;
        }

        public static byte[] Read(RestRequest request)
        {
            using (var memory = new MemoryStream())
            {
                request.InputStream.CopyTo(memory);

                var binary = memory.ToArray();

                return binary;
            }
        }
        #endregion
    }
}