using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LiteNetLib;

namespace Backend
{
    class LiteNetLibTransport : AutoDistributedNetworkTransport<LiteNetLibTransport, LiteNetLibTransportContext, LiteNetLibTransportClient, NetPeer, int>
    {
        public NetManager Server { get; protected set; }

        public EventBasedNetListener Listener { get; protected set; }

        public int Port { get; protected set; }

        public override void Start()
        {
            Listener.PeerConnectedEvent += x => Log.Info(x.EndPoint);

            Server.Start(Port);
        }

        protected override int GetIID(NetPeer connection) => connection.Id;

        protected override void Tick()
        {
            if (Server == null) return;
            if (Server.IsRunning == false) return;

            Server.PollEvents();
            Thread.Sleep(15);
        }

        #region Callbacks
        void ConnectionRequestCallback(ConnectionRequest request) => request.Accept();

        void PeerConnectCallback(NetPeer peer) => MarkUnregisteredConnection(peer);

        void RecieveCallback(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var raw = new byte[reader.AvailableBytes];
            Buffer.BlockCopy(reader.RawData, reader.Position, raw, 0, reader.AvailableBytes);

            ProcessMessage(peer, raw);
        }

        void PeerDisconnectCallback(NetPeer peer, DisconnectInfo disconnectInfo) => RemoveConnection(peer);
        #endregion

        protected override LiteNetLibTransportContext Create(uint id) => new LiteNetLibTransportContext(this, id);

        protected override void Send(NetPeer connection, byte[] raw) => connection.Send(raw, DeliveryMethod.ReliableOrdered);

        protected override void Disconnect(NetPeer connection) => connection.Disconnect();

        public LiteNetLibTransport(int port)
        {
            this.Port = port;

            Listener = new EventBasedNetListener();

            Listener.ConnectionRequestEvent += ConnectionRequestCallback;
            Listener.PeerConnectedEvent += PeerConnectCallback;
            Listener.NetworkReceiveEvent += RecieveCallback;
            Listener.PeerDisconnectedEvent += PeerDisconnectCallback;

            Server = new NetManager(Listener);
        }
    }

    class LiteNetLibTransportContext : NetworkTransportContext<LiteNetLibTransport, LiteNetLibTransportContext, LiteNetLibTransportClient, NetPeer, int>
    {
        public NetManager Server => Transport.Server;

        public override void Disconnect(LiteNetLibTransportClient client)
        {
            client.Connection.Disconnect();
        }

        public override void Send(LiteNetLibTransportClient client, byte[] raw)
        {
            client.Peer.Send(raw, DeliveryMethod.ReliableOrdered);
        }
        
        protected override LiteNetLibTransportClient CreateClient(NetworkClientID clientID, NetPeer connection)
        {
            var client = new LiteNetLibTransportClient(this, clientID, connection);

            return client;
        }

        public LiteNetLibTransportContext(LiteNetLibTransport transport, uint id) : base(transport, id)
        {

        }
    }

    class LiteNetLibTransportClient : NetworkTransportClient<LiteNetLibTransportContext, NetPeer, int>
    {
        public override int InternalID => Connection.Id;

        public NetPeer Peer => Connection;

        public LiteNetLibTransportClient(LiteNetLibTransportContext context, NetworkClientID clientID, NetPeer peer) : base(context, clientID, peer)
        {

        }
    }
}