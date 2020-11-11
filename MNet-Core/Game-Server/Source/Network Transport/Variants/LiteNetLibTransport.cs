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
    class LiteNetLibTransport : NetworkTransport<LiteNetLibTransport, LiteNetLibTransportContext, LiteNetLibTransportClient, NetPeer, int>
    {
        public override void Start()
        {

        }

        protected override LiteNetLibTransportContext Create(uint id) => new LiteNetLibTransportContext(this, id);

        public LiteNetLibTransport()
        {

        }
    }

    class LiteNetLibTransportContext : NetworkTransportContext<LiteNetLibTransport, LiteNetLibTransportContext, LiteNetLibTransportClient, NetPeer, int>, INetEventListener
    {
        public NetManager Manager { get; protected set; }

        public ushort Port { get; protected set; }

        public override void Poll()
        {
            Manager.PollEvents();

            base.Poll();
        }

        #region Callbacks
        public void OnPeerConnected(NetPeer peer) => RegisterClient(peer);

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) => UnregisterClient(peer);

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var raw = new byte[reader.AvailableBytes];
            Buffer.BlockCopy(reader.RawData, reader.Position, raw, 0, reader.AvailableBytes);
            reader.Recycle();

            var mode = Utility.Delivery.Glossary[deliveryMethod];

            RegisterMessage(peer, raw, mode);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

        public void OnConnectionRequest(ConnectionRequest request) => request.Accept();
        #endregion

        public override void Send(LiteNetLibTransportClient client, byte[] raw, DeliveryMode mode)
        {
            var method = Utility.Delivery.Glossary[mode];

            client.Peer.Send(raw, method);
        }

        public override void Broadcast(byte[] raw, DeliveryMode mode)
        {
            var method = Utility.Delivery.Glossary[mode];

            Manager.SendToAll(raw, method);
        }

        public override void Disconnect(LiteNetLibTransportClient client, DisconnectCode code)
        {
            var binary = Utility.Disconnect.CodeToBinary(code);

            Manager.DisconnectPeer(client.Peer, binary);
        }

        public override void Close()
        {
            base.Close();

            Manager.Stop(true);
        }

        protected override LiteNetLibTransportClient CreateClient(NetworkClientID clientID, NetPeer connection)
        {
            var client = new LiteNetLibTransportClient(this, clientID, connection);

            return client;
        }

        public LiteNetLibTransportContext(LiteNetLibTransport transport, uint id) : base(transport, id)
        {
            Manager = new NetManager(this);

            Port = NetworkTransportUtility.Port.From(id);
            Log.Info($"LiteNetLib Context {id} Port: {Port}");

            Manager.Start(Port);
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