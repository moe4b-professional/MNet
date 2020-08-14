using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

using System.Net;

namespace Game.Server
{
    class WebSockeAPI
    {
        public WebSocketServer Server { get; protected set; }

        public WebSocketServiceManager Services => Server.WebSocketServices;

        public void Configure(IPAddress address, int port)
        {
            Server = new WebSocketServer(address, port);

            Server.KeepClean = true;

            Server.Log.Level = LogLevel.Info;
            Server.Log.Output = (data, s) => { Log.Info(data.Message); };

            Server.AddWebSocketService<WebSocketAPIService>("/");

            Log.Info($"Configuring Rest API on {address}:{port}");
        }

        public void Start()
        {
            Server.Start();

            Log.Info("Starting WebSocket API");
        }

        public void AddService<TBehaviour>(string path)
            where TBehaviour : WebSocketBehavior, new()
        {
            Server.AddWebSocketService<TBehaviour>(path);
        }
    }

    class WebSocketAPIService : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            base.OnOpen();

            Log.Info($"WebSocket Client Connected: {Context.UserEndPoint.Address}");
        }

        protected override void OnMessage(MessageEventArgs args)
        {
            base.OnMessage(args);

            Log.Info($"WebSocket Client Message: \"{args.Data}\" from {Context.UserEndPoint.Address}");

            Context.WebSocket.Send("Welcome to WebSocket API");
        }

        protected override void OnClose(CloseEventArgs args)
        {
            base.OnClose(args);

            Log.Info($"WebSocket Client Disconnected With Code: {args.Code}");
        }

        public WebSocketAPIService()
        {

        }
    }
}