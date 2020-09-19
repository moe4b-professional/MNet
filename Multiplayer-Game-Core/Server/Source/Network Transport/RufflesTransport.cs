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
    class RufflesTransport : AutoDistributedNetworkTransport<RufflesTransport, RufflesTransportContext, RufflesTransportClient, Connection, ulong>
    {
        public RuffleSocket Socket { get; protected set; }

        public SocketConfig Config { get; protected set; }

        public ushort Port { get; protected set; }

        public const int PeerLimit = 1000; //TODO Increase

        public override void Start()
        {
            Socket.Start();
        }

        protected override ulong GetIID(Connection connection) => connection.Id;

        NetworkEvent rEvent;
        protected override void Tick()
        {
            if (Socket == null) return;
            if (Socket.IsRunning == false) return;

            rEvent = Socket.Poll();
            RouteEvent(rEvent);
            rEvent.Recycle();

            Thread.Sleep(15);
        }

        void RouteEvent(NetworkEvent rEvent)
        {
            switch (rEvent.Type)
            {
                case NetworkEventType.Nothing:
                    break;

                case NetworkEventType.Connect:
                    MarkUnregisteredConnection(rEvent.Connection);
                    break;

                case NetworkEventType.Disconnect:
                    RemoveConnection(rEvent.Connection);
                    break;

                case NetworkEventType.Data:
                    var raw = rEvent.Data.ToArray();
                    ProcessMessage(rEvent.Connection, raw);
                    break;

                case NetworkEventType.Timeout:
                    break;
            }
        }

        protected override RufflesTransportContext Create(uint id) => new RufflesTransportContext(this, id);

        protected override void Send(Connection connection, byte[] raw) => connection.Send(raw, 0, false, 0);

        protected override void Disconnect(Connection connection) => connection.Disconnect(true);

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
        }
    }

    class RufflesTransportContext : NetworkTransportContext<RufflesTransport, RufflesTransportContext, RufflesTransportClient, Connection, ulong>
    {
        protected override RufflesTransportClient CreateClient(NetworkClientID clientID, Connection session)
        {
            var client = new RufflesTransportClient(this, clientID, session);

            return client;
        }

        public override void Send(RufflesTransportClient client, byte[] raw)
        {
            client.Connection.Send(raw, 1, false, 0);
        }

        public override void Disconnect(RufflesTransportClient client)
        {
            client.Connection.Disconnect(true);
        }

        public RufflesTransportContext(RufflesTransport transport, uint id) : base(transport, id)
        {
            
        }
    }

    class RufflesTransportClient : NetworkTransportClient<RufflesTransportContext, Connection, ulong>
    {
        public override ulong InternalID => Connection.Id;

        public RufflesTransportClient(RufflesTransportContext context, NetworkClientID clientID, Connection connect) : base(context, clientID, connect)
        {

        }
    }
}