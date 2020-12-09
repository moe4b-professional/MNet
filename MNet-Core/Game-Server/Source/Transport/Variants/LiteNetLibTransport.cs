using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;

using LiteNetLib;

using Utility = MNet.NetworkTransportUtility.LiteNetLib;

using System.Collections.Concurrent;

namespace MNet
{
    class LiteNetLibTransport : NetworkTransport<LiteNetLibTransport, LiteNetLibTransportContext, LiteNetLibTransportClient, NetPeer, int>, INetEventListener
    {
        public override int MTU => Utility.MTU;

        public NetManager Manager { get; protected set; }

        public static ushort Port => Constants.Server.Game.Realtime.Port;

        public ConcurrentDictionary<int, LiteNetLibTransportContext> Routes { get; protected set; }

        public override void Start()
        {
            Manager.Start(Port);
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
            Thread.Sleep(1);
        }

        #region Callbacks
        public void OnConnectionRequest(ConnectionRequest request)
        {
            var key = request.Data.GetString();

            if (RoomID.TryParse(key, out var room) == false)
            {
                Reject(request, DisconnectCode.InvalidContext);
                return;
            }

            if (Contexts.TryGetValue(room.Value, out var context) == false)
            {
                Reject(request, DisconnectCode.InvalidContext);
                return;
            }

            var peer = request.Accept();

            Routes[peer.Id] = context;
        }

        public void OnPeerConnected(NetPeer peer)
        {
            if(Routes.TryGetValue(peer.Id, out var context) == false)
            {
                Log.Warning($"Peer {peer.Id} not Registered with any Context Route");
                return;
            }

            context.RegisterClient(peer);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (Routes.TryGetValue(peer.Id, out var context) == false)
            {
                Log.Warning($"Peer {peer.Id} not Registered with any Context Route");
                return;
            }

            var raw = reader.GetRemainingBytes();
            reader.Recycle();

            var mode = Utility.Delivery.Glossary[deliveryMethod];

            context.RegisterMessages(peer, raw, mode);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (Routes.TryGetValue(peer.Id, out var context) == false)
            {
                Log.Warning($"Peer {peer.Id} not Registered with any Context Route");
                return;
            }

            context.UnregisterClient(peer);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        #endregion

        public void Reject(ConnectionRequest request, DisconnectCode code)
        {
            var binary = Utility.Disconnect.CodeToBinary(DisconnectCode.InvalidContext);

            request.Reject(binary);
        }

        protected override LiteNetLibTransportContext Create(uint id) => new LiteNetLibTransportContext(this, id);

        public LiteNetLibTransport()
        {
            Manager = new NetManager(this);

            Routes = new ConcurrentDictionary<int, LiteNetLibTransportContext>();

            new Thread(Run).Start();
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
            var binary = Utility.Disconnect.CodeToBinary(code);

            client.Peer.Disconnect(binary);
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