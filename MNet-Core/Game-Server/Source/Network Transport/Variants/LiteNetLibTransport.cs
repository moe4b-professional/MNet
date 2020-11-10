using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

using LiteNetLib;

namespace MNet
{
    class LiteNetLibTransport : AutoDistributedNetworkTransport<LiteNetLibTransport, LiteNetLibTransportContext, LiteNetLibTransportClient, NetPeer, int>, INetEventListener
    {
        public NetManager Server { get; protected set; }

        public int Port { get; protected set; }

        public override void Start()
        {
            Server.Start(Port);
        }

        protected override int GetIID(NetPeer connection) => connection.Id;

        protected override LiteNetLibTransportContext Create(uint id) => new LiteNetLibTransportContext(this, id);

        protected override void Tick()
        {
            if (Server == null) return;
            if (Server.IsRunning == false) return;

            Server.PollEvents();
            Thread.Sleep(15);
        }

        #region Callbacks
        public void OnPeerConnected(NetPeer peer) => MarkUnregisteredConnection(peer);

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) => RemoveConnection(peer);

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var raw = new byte[reader.AvailableBytes];
            Buffer.BlockCopy(reader.RawData, reader.Position, raw, 0, reader.AvailableBytes);

            var channel = DeliveryGlossary[deliveryMethod];

            ProcessMessage(peer, raw, channel);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

        public void OnConnectionRequest(ConnectionRequest request) => request.Accept();
        #endregion

        protected override void Send(NetPeer connection, byte[] raw) => connection.Send(raw, DeliveryMethod.ReliableOrdered);

        public override void Disconnect(NetPeer connection, DisconnectCode code)
        {
            var binary = DisconnectCodeToBinary(code);

            connection.Disconnect(binary);
        }

        public LiteNetLibTransport(int port)
        {
            this.Port = port;

            Server = new NetManager(this);
        }

        public static byte[] DisconnectCodeToBinary(DisconnectCode code)
        {
            var value = Convert.ToByte(code);

            return new byte[] { value };
        }

        //Static Utility

        public static Glossary<DeliveryChannel, DeliveryMethod> DeliveryGlossary { get; private set; }

        static LiteNetLibTransport()
        {
            DeliveryGlossary = new Glossary<DeliveryChannel, DeliveryMethod>();

            DeliveryGlossary.Add(DeliveryChannel.Reliable, DeliveryMethod.ReliableOrdered);
            DeliveryGlossary.Add(DeliveryChannel.Unreliable, DeliveryMethod.Unreliable);
        }
    }

    class LiteNetLibTransportContext : NetworkTransportContext<LiteNetLibTransport, LiteNetLibTransportContext, LiteNetLibTransportClient, NetPeer, int>
    {
        public NetManager Server => Transport.Server;

        public override void Send(LiteNetLibTransportClient client, byte[] raw, DeliveryChannel channel = DeliveryChannel.Reliable)
        {
            var method = LiteNetLibTransport.DeliveryGlossary[channel];

            client.Peer.Send(raw, method);
        }

        public override void Disconnect(LiteNetLibTransportClient client, DisconnectCode code = DisconnectCode.Normal) => Transport.Disconnect(client.Peer, code);

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