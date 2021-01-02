using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace MNet
{
    static class RealtimeAPI
    {
        public static INetworkTransport Transport { get; private set; }

        public const int Port = Constants.Server.Game.Realtime.Port;

        public static void Configure(NetworkTransportType type)
        {
            switch (type)
            {
                case NetworkTransportType.WebSockets:
                    Transport = new WebSocketTransport();
                    break;

                case NetworkTransportType.LiteNetLib:
                    Transport = new LiteNetLibTransport();
                    break;
            }
        }

        public static void Start()
        {
            Log.Info($"Starting {Transport.GetType().Name}");

            Transport.Start();
        }

        public static INetworkTransportContext Register(uint code) => Transport.Register(code);

        public static void Unregister(uint code) => Transport.Unregister(code);
    }
}