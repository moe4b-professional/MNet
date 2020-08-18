using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Server;

using System.IO;
using System.Net;

using Game.Shared;

using HttpRequest = WebSocketSharp.Net.HttpListenerRequest;
using HttpResponse = WebSocketSharp.Net.HttpListenerResponse;

using HttpCode = WebSocketSharp.Net.HttpStatusCode;

namespace Game.Server
{
    class RestAPI
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
                WriteTo(response, HttpCode.NotFound, "Error 404");
        }

        public static void WriteTo(HttpResponse response, HttpCode code, string message)
        {
            response.StatusCode = (int)code;

            response.ContentEncoding = Encoding.UTF8;
            var data = Encoding.UTF8.GetBytes(message);
            response.WriteContent(data);

            response.Close();
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
    }

    class RestAPIRouter
    {
        public List<ProcessDelegate> List { get; protected set; }
        
        public virtual bool Process(HttpRequest request, WebSocketSharp.Net.HttpListenerResponse response)
        {
            for (int i = 0; i < List.Count; i++)
                if (List[i](request, response))
                    return true;

            return false;
        }

        public virtual void Register(ProcessDelegate callback)
        {
            List.Add(callback);
        }

        public delegate bool CheckDelegate(HttpRequest request);
        public delegate bool ProcessDelegate(HttpRequest request, HttpResponse response);

        public RestAPIRouter()
        {
            List = new List<ProcessDelegate>();
        }
    }
}