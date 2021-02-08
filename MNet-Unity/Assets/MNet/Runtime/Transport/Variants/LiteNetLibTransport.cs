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
    public class LiteNetLibTransport : NetworkTransport, INetEventListener
    {
        public NetManager Client { get; protected set; }

        public NetPeer Peer { get; protected set; }

        public override bool IsConnected
        {
            get
            {
                if (Peer == null) return false;

                return Peer.ConnectionState == ConnectionState.Connected;
            }
        }

        public const ushort Port = Utility.Port;

        public override int CheckMTU(DeliveryMode mode) => Utility.CheckMTU(mode);

        public override void Connect(GameServerID server, RoomID room)
        {
            var key = $"{room}";

            Client.Start();

            Peer = Client.Connect(server.Address, Port, key);
        }

        void Run()
        {
            while (NetworkAPI.IsRunning) Tick();
        }

        void Tick()
        {
            Client.PollEvents();

            Thread.Sleep(1);
        }

        #region Callbacks
        public void OnPeerConnected(NetPeer peer) => InvokeConnect();

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var raw = reader.GetRemainingBytes();
            reader.Recycle();

            var mode = Utility.Delivery.Glossary[deliveryMethod];

            InvokeMessages(raw, mode);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            var code = Utility.Disconnect.InfoToCode(info);

            InvokeDisconnect(code);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnConnectionRequest(ConnectionRequest request) { }
        #endregion

        public override void Send(byte[] raw, DeliveryMode mode, byte channel)
        {
            var method = Utility.Delivery.Glossary[mode];

            Peer.Send(raw, channel, method);
        }

        public override void Close()
        {
            Peer.Disconnect();
            Client.Stop(true);
        }

        public LiteNetLibTransport()
        {
            Client = new NetManager(this);
            Client.ChannelsCount = 64;
            Client.UpdateTime = 1;

            new Thread(Run).Start();
        }
    }
}