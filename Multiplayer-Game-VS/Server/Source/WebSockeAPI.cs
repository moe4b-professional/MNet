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
        WebSocketServer server;

        WebSocketService service;

        public void Configure(IPAddress address, int port)
        {
            server = new WebSocketServer(address, port);

            server.KeepClean = true;

            server.Log.Level = LogLevel.Info;
            server.Log.Output = (data, s) => { Log.Info(data.Message); };

            server.AddWebSocketService("/", InitializeService);

            Log.Info($"Configuring Rest API on {address}:{port}");
        }

        WebSocketService InitializeService()
        {
            service = new WebSocketService();

            return service;
        }

        public void Start()
        {
            server.Start();

            Log.Info("Starting WebSocket API");
        }
    }

    class WebSocketService : WebSocketBehavior
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

            Context.WebSocket.Send("Welcome to the API");
        }

        public WebSocketService()
        {

        }
    }
}
