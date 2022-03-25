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
        public NetManager Server { get; protected set; }

        public const ushort Port = Utility.Port;

        public override int CheckMTU(DeliveryMode mode) => Utility.CheckMTU(mode);

        public override void Start()
        {
            Server.Start(Port);
        }

        void Run()
        {
            while (true) Tick();
        }

        void Tick()
        {
            if (Server == null) return;
            if (Server.IsRunning == false) return;

            Server.PollEvents();
            Thread.Sleep(1);
        }

        #region Callbacks
        public void OnConnectionRequest(ConnectionRequest request)
        {
            if (request.Data.TryGetString(out var key) == false)
            {
                Reject(request, DisconnectCode.ConnectionRejected);
                return;
            }
            if (RoomID.TryParse(key, out var room) == false)
            {
                Reject(request, DisconnectCode.ConnectionRejected);
                return;
            }
            if (Contexts.TryGetValue(room.Value, out var context) == false)
            {
                Reject(request, DisconnectCode.InvalidContext);
                return;
            }

            var peer = request.Accept();

            var tag = new LiteNetLibClientTag();
            tag.Context = context;

            peer.Tag = tag;
        }

        public void OnPeerConnected(NetPeer peer)
        {
            var tag = peer.Tag as LiteNetLibClientTag;

            tag.Client = tag.Context.RegisterClient(peer);
        }
        public void OnNetworkReceive(NetPeer peer, NetPacketReader packet, DeliveryMethod delivery, byte channel)
        {
            var tag = peer.Tag as LiteNetLibClientTag;

            if (Utility.Delivery.Glossary.TryGetKey(delivery, out var mode) == false)
            {
                Log.Error($"LiteNetLib: Recieved Packet with Undefined Delivery Method of {delivery}");
                Disconnect(peer, DisconnectCode.InvalidData);
                return;
            }

            var segment = packet.GetRemainingBytesSegment();

            tag.Context.InvokeMessage(tag.Client, segment, mode, channel, packet.Recycle);
        }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            var tag = peer.Tag as LiteNetLibClientTag;
            tag.Context.UnregisterClient(tag.Client);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader packet, UnconnectedMessageType messageType) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        #endregion

        protected override LiteNetLibTransportContext CreateContext(uint id) => new LiteNetLibTransportContext(this, id);

        public override void Stop()
        {
            var binary = Utility.Disconnect.CodeToBinary(DisconnectCode.ServerClosed);

            Server.DisconnectAll(binary, 0, binary.Length);
            Server.Stop();
        }

        public LiteNetLibTransport()
        {
            Server = new NetManager(this);
            Server.ChannelsCount = 64;
            Server.UpdateTime = 1;

            new Thread(Run).Start();
        }

        //Static Utility

        public static void Reject(ConnectionRequest request, DisconnectCode code)
        {
            var binary = Utility.Disconnect.CodeToBinary(code);

            request.Reject(binary);
        }

        public static void Disconnect(NetPeer peer, DisconnectCode code)
        {
            var binary = Utility.Disconnect.CodeToBinary(code);

            peer.Disconnect(binary);
        }
    }

    class LiteNetLibTransportContext : NetworkTransportContext<LiteNetLibTransport, LiteNetLibTransportContext, LiteNetLibTransportClient, NetPeer, int>
    {
        protected override LiteNetLibTransportClient CreateClient(NetworkClientID clientID, NetPeer connection)
        {
            return new LiteNetLibTransportClient(this, clientID, connection);
        }

        public override void Send(LiteNetLibTransportClient client, ArraySegment<byte> segment, DeliveryMode mode, byte channel)
        {
            var method = Utility.Delivery.Glossary[mode];

            client.Peer.Send(segment.Array, segment.Offset, segment.Count, channel, method);
        }

        public override void Disconnect(LiteNetLibTransportClient client, DisconnectCode code)
        {
            var binary = Utility.Disconnect.CodeToBinary(code);

            client.Peer.Disconnect(binary);
        }

        public override void Close()
        {

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

    class LiteNetLibClientTag
    {
        public LiteNetLibTransportContext Context;
        public LiteNetLibTransportClient Client;
    }
}