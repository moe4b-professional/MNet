using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

using Ruffles;
using Ruffles.Core;
using Ruffles.Configuration;
using Ruffles.Channeling;
using Ruffles.Connections;

namespace Backend
{
    class RufflesTransport : NetworkTransport<RufflesTransport, RufflesTransportContext, RufflesTransportClient, Connection, ulong>
    {
        public RuffleSocket Socket { get; protected set; }

        public SocketConfig Config { get; protected set; }

        public ushort Port { get; protected set; }

        public const int PeerLimit = 1000;

        #region Unregisterd Clients
        public HashSet<ulong> UnregisteredClients { get; protected set; }

        public bool IsUnregistered(Connection connection) => IsUnregistered(connection.Id);
        public bool IsUnregistered(ulong connectionID) => UnregisteredClients.Contains(connectionID);

        void UnregisterConnection(NetworkEvent rEvent) => UnregisteredClients.Add(rEvent.Connection.Id);

        bool RegisterConnection(NetworkEvent rEvent) => UnregisteredClients.Remove(rEvent.Connection.Id);

        public readonly byte[] RegisterClientPayload = new byte[] { 200 };
        #endregion

        public Dictionary<ulong, RufflesTransportClient> Clients { get; protected set; }

        public override void Start()
        {
            Socket.Start();

            thread = new Thread(Tick);
            thread.Start();
        }

        Thread thread;
        void Tick()
        {
            while (true)
            {
                var rEvent = Socket.Poll();

                RouteEvent(rEvent);

                rEvent.Recycle();
            }
        }

        #region Events
        void RouteEvent(NetworkEvent rEvent)
        {
            switch (rEvent.Type)
            {
                case NetworkEventType.Nothing:
                    break;

                case NetworkEventType.Connect:
                    ConnectAction(rEvent);
                    break;

                case NetworkEventType.Disconnect:
                    DisconnectAction(rEvent);
                    break;

                case NetworkEventType.Data:
                    RecieveAction(rEvent);
                    break;

                case NetworkEventType.Timeout:
                    break;
            }
        }

        void ConnectAction(NetworkEvent rEvent)
        {
            Log.Info($"Client {rEvent.Connection.Id} Connected");

            UnregisterConnection(rEvent);
        }
        void RecieveAction(NetworkEvent rEvent)
        {
            Log.Info($"Client Message: {rEvent.Data.Count}");

            if (IsUnregistered(rEvent.Connection))
                AddClient(rEvent);
            else
                SendMessage(rEvent);
        }
        void DisconnectAction(NetworkEvent rEvent)
        {
            Log.Info($"Client {rEvent.Connection.Id} Disconnected");

            RemoveClient(rEvent);
        }
        #endregion

        #region Actions
        void AddClient(NetworkEvent rEvent)
        {
            uint contextID;

            try
            {
                var raw = rEvent.Data.ToArray();

                contextID = BitConverter.ToUInt32(raw);
            }
            catch (Exception)
            {
                rEvent.Connection.Disconnect(false);
                return;
            }

            if (Contexts.TryGetValue(contextID, out var context) == false)
            {
                Log.Warning($"Connection {rEvent.Connection.Id} Trying to Register to Non-Registered Context {contextID}");
                rEvent.Connection.Disconnect(true);
            }

            var client = context.RegisterClient(rEvent.Connection);

            RegisterConnection(rEvent);

            Clients.Add(client.InternalID, client);

            context.Send(client, RegisterClientPayload);
        }

        void SendMessage(NetworkEvent rEvent)
        {
            if (Clients.TryGetValue(rEvent.Connection.Id, out var client) == false)
            {
                Log.Info($"Client {rEvent.Connection.Id} Not Marked Unregistered but Also Not Registered");
                return;
            }

            var context = client.Context;

            var raw = rEvent.Data.ToArray();

            context.RegisterMessage(client, raw);
        }

        void RemoveClient(NetworkEvent rEvent)
        {
            if (Clients.TryGetValue(rEvent.Connection.Id, out var client) == false)
            {
                Log.Warning($"Client {rEvent.Connection.Id} Disconnected Without Being Registered");
                return;
            }

            var context = client.Context;

            context.UnregisterClient(client);

            Clients.Remove(client.InternalID);
        }
        #endregion

        protected override RufflesTransportContext Create(uint id)
        {
            var context = new RufflesTransportContext(this, id);

            return context;
        }

        public RufflesTransport(ushort port) : base()
        {
            var channels = new ChannelType[]
            {
                ChannelType.Reliable,
                ChannelType.ReliableSequenced,
                ChannelType.Unreliable,
                ChannelType.UnreliableOrdered,
                ChannelType.ReliableSequencedFragmented
            };

            Config = new SocketConfig()
            {
                ChallengeDifficulty = 20,
                DualListenPort = port,
                ChannelTypes = channels,
                TimeBasedConnectionChallenge = false,
            };
            Socket = new RuffleSocket(Config);

            UnregisteredClients = new HashSet<ulong>();

            Clients = new Dictionary<ulong, RufflesTransportClient>();
        }
    }

    class RufflesTransportContext : NetworkTransportContext<RufflesTransport, RufflesTransportClient, Connection, ulong>
    {
        protected override RufflesTransportClient CreateClient(NetworkClientID clientID, Connection session)
        {
            var client = new RufflesTransportClient(clientID, session, this);

            return client;
        }

        public override void Send(RufflesTransportClient client, byte[] raw)
        {
            client.Session.Send(raw, 1, false, 0);
        }

        public override void Disconnect(RufflesTransportClient client)
        {
            client.Session.Disconnect(true);
        }

        public RufflesTransportContext(RufflesTransport transport, uint id) : base(transport, id)
        {
            
        }
    }

    class RufflesTransportClient : NetworkTransportClient<Connection, ulong>
    {
        public RufflesTransportContext Context { get; protected set; }

        public override ulong InternalID => Session.Id;

        public RufflesTransportClient(NetworkClientID clientID, Connection connect, RufflesTransportContext context) : base(clientID, connect)
        {
            this.Context = context;
        }
    }
}