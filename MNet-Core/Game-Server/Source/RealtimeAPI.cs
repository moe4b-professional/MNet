using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace MNet
{
    class RealtimeAPI
    {
        public INetworkTransport Transport { get; protected set; }

        public const int Port = Constants.Server.Game.Realtime.Port;

        public virtual void Start()
        {
            Log.Info($"Starting {Transport.GetType().Name}");

            Transport.Start();
        }

        public virtual INetworkTransportContext Register(uint code) => Transport.Register(code);

        public virtual void Unregister(uint code) => Transport.Unregister(code);

        public RealtimeAPI(NetworkTransportType type)
        {
            switch (type)
            {
                case NetworkTransportType.WebSocketSharp:
                    Transport = new WebSocketTransport(Port);
                    break;

                case NetworkTransportType.LiteNetLib:
                    Transport = new LiteNetLibTransport(Port);
                    break;
            }
        }
    }
}