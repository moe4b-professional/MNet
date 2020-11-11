using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;

using LiteNetLib;

using Utility = MNet.NetworkTransportUtility.LiteNetLib;

namespace MNet
{
    class LiteNetLibTransport : DistributedNetworkTransport<LiteNetLibTransport, LiteNetLibTransportContext, LiteNetLibTransportClient, NetPeer, int>, INetEventListener
    {
        public NetManager Manager { get; protected set; }

        public static ushort Port => Constants.Server.Game.Realtime.Port;

        public override void Start()
        {

        }

        void Run()
        {
            while (true) Tick();
        }

        void Tick()
        {
            if (Manager == null) return;
            if (Manager.IsRunning == false) return;

            Manager.PollEvents();
            Thread.Sleep(10);
        }

        #region Callbacks
        public void OnPeerConnected(NetPeer peer) => MarkUnregisteredConnection(peer);

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) => RemoveConnection(peer);

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var raw = new byte[reader.AvailableBytes];
            Buffer.BlockCopy(reader.RawData, reader.Position, raw, 0, reader.AvailableBytes);
            reader.Recycle();

            var mode = Utility.Delivery.Glossary[deliveryMethod];

            ProcessMessage(peer, raw, mode);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

        public void OnConnectionRequest(ConnectionRequest request) => request.Accept();
        #endregion

        protected override int GetIID(NetPeer connection) => connection.Id;

        protected override LiteNetLibTransportContext Create(uint id) => new LiteNetLibTransportContext(this, id);

        protected override void Send(NetPeer connection, params byte[] raw) => connection.Send(raw, DeliveryMethod.ReliableOrdered);

        public override void Disconnect(NetPeer connection, DisconnectCode code)
        {
            var binary = Utility.Disconnect.CodeToBinary(code);

            connection.Disconnect(binary);
        }

        public LiteNetLibTransport()
        {
            Manager = new NetManager(this);
            Manager.Start(Port);

            var thread = new Thread(Run);
            thread.Start();
        }
    }

    class LiteNetLibTransportContext : NetworkTransportContext<LiteNetLibTransport, LiteNetLibTransportContext, LiteNetLibTransportClient, NetPeer, int>
    {
        public override void Send(LiteNetLibTransportClient client, byte[] raw, DeliveryMode mode)
        {
            var method = Utility.Delivery.Glossary[mode];

            client.Peer.Send(raw, method);
        }

        public override void Disconnect(LiteNetLibTransportClient client, DisconnectCode code)
        {
            Transport.Disconnect(client.Peer, code);
        }

        protected override LiteNetLibTransportClient CreateClient(NetworkClientID clientID, NetPeer connection)
        {
            return new LiteNetLibTransportClient(this, clientID, connection);
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