using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net;

using WebSocketSharp;
using WebSocketSharp.Server;

using HttpListenerRequest = WebSocketSharp.Net.HttpListenerRequest;
using HttpListenerResponse = WebSocketSharp.Net.HttpListenerResponse;

namespace Game.Server
{
    class RestAPI
    {
        public HttpServer Server { get; protected set; }

        public RestAPIRouter Router { get; protected set; }

        public void Configure(IPAddress address, int port)
        {
            Server = new HttpServer(address, port);

            Server.OnGet += RequestCallback;
            Server.OnPost += RequestCallback;
            Server.OnDelete += RequestCallback;
            Server.OnPut += RequestCallback;

            Router = new RestAPIRouter();

            Log.Info($"Configuring Rest API on {address}:{port}");
        }

        public void Start()
        {
            Log.Info("Starting Rest API");

            Server.Start();
        }

        void RequestCallback(object sender, HttpRequestEventArgs args)
        {
            var request = args.Request;
            var response = args.Response;

            Log.Info($"Rest API Request: {request.HttpMethod}:{request.Url.AbsolutePath} from {request.UserHostAddress}");

            if (Router.Process(request, response))
            {
                
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;

                response.ContentEncoding = Encoding.UTF8;
                var data = Encoding.UTF8.GetBytes("ERROR 404, NOT FOUND");
                response.WriteContent(data);

                response.Close();
            }
        }
    }

    class RestAPIRouter
    {
        public List<ProcessDelegate> List { get; protected set; }
        
        public virtual bool Process(HttpListenerRequest request, WebSocketSharp.Net.HttpListenerResponse response)
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

        public delegate bool CheckDelegate(HttpListenerRequest request);
        public delegate bool ProcessDelegate(HttpListenerRequest request, HttpListenerResponse response);

        public RestAPIRouter()
        {
            List = new List<ProcessDelegate>();
        }
    }
}