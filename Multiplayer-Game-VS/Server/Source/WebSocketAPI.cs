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
    class WebSocketAPI
    {
        public WebSocketServer Server { get; protected set; }

        public WebSocketServiceManager Services => Server.WebSocketServices;

        public void Start()
        {
            Log.Info($"Starting {nameof(WebSocketAPI)}");

            Server.Start();
        }

        public void AddService<TBehaviour>(string path, Func<TBehaviour> initializer)
            where TBehaviour : WebSocketBehavior, new()
        {
            Server.AddWebSocketService(path, initializer);
        }

        public void RemoveService(string path) => Server.RemoveWebSocketService(path);

        public WebSocketAPI(IPAddress address, int port)
        {
            Log.Info($"Configuring {nameof(WebSocketAPI)} on {address}:{port}");

            Server = new WebSocketServer(address, port);

            Server.KeepClean = true;

            Server.Log.Level = LogLevel.Info;
            Server.Log.Output = (data, s) => { Log.Info(data.Message); };
        }
    }
}