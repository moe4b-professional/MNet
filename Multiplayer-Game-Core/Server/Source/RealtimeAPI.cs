using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Backend
{
    class RealtimeAPI
    {
        public NetworkTransport Transport { get; protected set; }

        public virtual void Start()
        {
            Log.Info($"Starting {Transport.GetType().Name}");

            Transport.Start();
        }

        public virtual NetworkTransportContext Register(uint code) => Transport.Register(code);

        public virtual void Unregister(uint code) => Transport.Unregister(code);

        public RealtimeAPI(NetworkTransport transport)
        {
            this.Transport = transport;
        }
    }
}