﻿using System;
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
                if (Peer == null)
                    return false;

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
            while (NetworkAPI.IsRunning)
                Tick();
        }
        void Tick()
        {
            Client.PollEvents();

            Thread.Sleep(1);
        }

        public override void Send(ArraySegment<byte> segment, DeliveryMode mode, byte channel)
        {
            var method = Utility.Delivery.Glossary[mode];

            Peer.Send(segment.Array, segment.Offset, segment.Count, channel, method);
        }

        public override void Disconnect(DisconnectCode code)
        {
            var data = Utility.Disconnect.CodeToBinary(code);

            Peer.Disconnect(data);
        }

        #region Callbacks
        public void OnPeerConnected(NetPeer peer) => InvokeConnect();
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod, byte channel)
        {
            var mode = Utility.Delivery.Glossary[deliveryMethod];

            var segment = reader.GetRemainingBytesSegment();

            InvokeMessage(segment, mode, reader.Recycle);
        }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            var code = Utility.Disconnect.InfoToCode(info);

            Client.Stop(true);

            InvokeDisconnect(code);
        }

        public void OnConnectionRequest(ConnectionRequest request) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }
        #endregion

        public LiteNetLibTransport()
        {
            Client = new NetManager(this);
            Client.ChannelsCount = NetworkAPI.Channels.Max;
            Client.UpdateTime = 1;

            new Thread(Run).Start();
        }
    }
}