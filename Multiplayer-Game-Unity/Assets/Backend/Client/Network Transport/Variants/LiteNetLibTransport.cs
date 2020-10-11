using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using LiteNetLib;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace Backend
{
    public class LiteNetLibTransport : AutoDistributedNetworkTransport, INetEventListener
    {
        public NetManager Client { get; protected set; }

        public NetPeer Peer { get; protected set; }

        public override bool IsConnected
        {
            get
            {
                if (IsRegistered == false) return false;

                if (Peer == null) return false;

                return Peer.ConnectionState == ConnectionState.Connected;
            }
        }

        public override void Connect(GameServerID serverID, RoomID roomID)
        {
            base.Connect(serverID, roomID);

            Client.Start();

            Peer = Client.Connect(serverID.Address, Port, "");
        }

        protected override void Tick()
        {
            if (Client == null) return;
            if (Client.IsRunning == false) return;

            Client.PollEvents();

            Thread.Sleep(15);
        }

        #region Callbacks
        public void OnPeerConnected(NetPeer peer) => RequestRegister();

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) => QueueDisconnect();

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var raw = new byte[reader.AvailableBytes];
            Buffer.BlockCopy(reader.RawData, reader.Position, raw, 0, reader.AvailableBytes);

            ProcessMessage(raw);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

        public void OnConnectionRequest(ConnectionRequest request) { }
        #endregion

        public override void Send(byte[] raw)
        {
            Peer.Send(raw, DeliveryMethod.ReliableOrdered);
        }

        public override void Close()
        {
            Client.Stop();

            Peer.Disconnect();
        }

        public LiteNetLibTransport()
        {
            Client = new NetManager(this);
        }
    }
}