﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

using System.Net;

using Game.Shared;

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

        public void AddService<TBehaviour>(string path, Action<TBehaviour> initializer)
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

    public static class WebSocketAPIExtensions
    {
        public static void Broadcast(this WebSocketSessionManager manager, NetworkMessage message)
        {
            var binary = NetworkSerializer.Serialize(message);

            manager.Broadcast(binary);
        }

        public static void Send(this WebSocketSessionManager manager, NetworkMessage message, string id)
        {
            var binary = NetworkSerializer.Serialize(message);

            manager.SendTo(binary, id);
        }
    }
}