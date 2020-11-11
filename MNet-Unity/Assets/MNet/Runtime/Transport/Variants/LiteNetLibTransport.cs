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

using Utility = MNet.NetworkTransportUtility.LiteNetLib;

using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace MNet
{
    public class LiteNetLibTransport : DistributedNetworkTransport, INetEventListener
    {
        public NetManager Client { get; protected set; }

        public NetPeer Peer { get; protected set; }

        public static ushort Port => Constants.Server.Game.Realtime.Port;

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
            Peer = Client.Connect(serverID.Address, Port, string.Empty);
        }

        void Update()
        {
            if (Client == null) return;

            Client.PollEvents();
        }

        #region Callbacks
        public void OnPeerConnected(NetPeer peer) => RequestRegisteration();

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            Debug.Log($"Internal LiteNetLib Disconnect Code: {info.Reason}");

            var code = Utility.Disconnect.InfoToCode(info);

            QueueDisconnect(code);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var raw = new byte[reader.AvailableBytes];
            Buffer.BlockCopy(reader.RawData, reader.Position, raw, 0, reader.AvailableBytes);
            reader.Recycle();

            var mode = Utility.Delivery.Glossary[deliveryMethod];

            ProcessMessage(raw, mode);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnConnectionRequest(ConnectionRequest request) { }
        #endregion

        public override void Send(byte[] raw, DeliveryMode mode)
        {
            var method = Utility.Delivery.Glossary[mode];

            Peer.Send(raw, method);
        }

        public override void Close() => Peer.Disconnect();

        void ApplicationQuitCallback()
        {
            NetworkAPI.OnUpdate -= Update;
        }

        public LiteNetLibTransport()
        {
            Client = new NetManager(this);
            Client.Start();

            NetworkAPI.OnUpdate += Update;

            Application.quitting += ApplicationQuitCallback;
        }
    }
}