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

            Log.Info($"Configuring Rest API on {address}:{port}");
        }

        public void Start()
        {
            Server.Start();

            Log.Info("Starting WebSocket API");
        }

        public void AddService<TBehaviour>(string path, Func<TBehaviour> initializer)
            where TBehaviour : WebSocketBehavior, new()
        {
            Server.AddWebSocketService(path, initializer);
        }
    }
}