using System;
using System.Text;
using System.Net;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace MNet
{
    static class Realtime
    {
        public static Dictionary<NetworkTransportType, INetworkTransport> Transports { get; private set; }

        public static void Configure()
        {
            Transports = new Dictionary<NetworkTransportType, INetworkTransport>();

            var types = AppsAPI.Dictionary.Values.Select(x => x.Transport).Distinct();

            foreach (var type in types)
            {
                Log.Info($"Configuring {type} Transport");

                var transport = CreateTransport(type);

                Transports.Add(type, transport);
            }
        }

        static INetworkTransport CreateTransport(NetworkTransportType type)
        {
            switch (type)
            {
                case NetworkTransportType.WebSockets:
                    return new WebSocketTransport();

                case NetworkTransportType.LiteNetLib:
                    return new LiteNetLibTransport();

                default:
                    throw new NotImplementedException($"No Realtime Transport Create Method Defined for {type}");
            }
        }

        public static void Start()
        {
            foreach (var pair in Transports)
            {
                var type = pair.Key;
                var transport = pair.Value;

                Log.Info($"Starting {type} Transport");

                transport.Start();
            }
        }

        public static INetworkTransportContext Register(NetworkTransportType type, uint code)
        {
            if (Transports.TryGetValue(type, out var transport) == false)
                throw new Exception($"No Transport of Type {type} Defined in Realtime");

            var context = transport.Register(code);

            return context;
        }

        public static void Unregister(NetworkTransportType type, uint code)
        {
            if (Transports.TryGetValue(type, out var transport) == false)
                throw new Exception($"No Transport of Type {type} Defined in Realtime");

            transport.Unregister(code);
        }

        public static void Close()
        {
            foreach (var transport in Transports.Values)
                transport.Close();
        }
    }
}