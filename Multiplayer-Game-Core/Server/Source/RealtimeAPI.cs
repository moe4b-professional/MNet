using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace Backend
{
    class RealtimeAPI
    {
        public INetworkTransport Transport { get; protected set; }

        public virtual void Start()
        {
            Log.Info($"Starting {Transport.GetType().Name}");

            Transport.Start();
        }

        public virtual INetworkTransportContext Register(uint code) => Transport.Register(code);

        public virtual void Unregister(uint code) => Transport.Unregister(code);

        public RealtimeAPI(IPAddress address)
        {
            Transport = new WebSocketTransport(address, Constants.RealtimeAPI.Port);
        }
    }
}