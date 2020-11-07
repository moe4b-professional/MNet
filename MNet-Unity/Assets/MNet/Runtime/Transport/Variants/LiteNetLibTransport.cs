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

using System.Threading;
using System.Net;
using System.Net.Sockets;

using LiteNetLib;

namespace MNet
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

            Peer = Client.Connect(serverID.Address, Port, "");
        }

        protected override void Tick()
        {
            if (Client == null) return;

            Client.PollEvents();

            Thread.Sleep(15);
        }

        #region Callbacks
        public void OnPeerConnected(NetPeer peer) => RequestRegister();

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            var code = InfoToDisconnectCode(info);

            QueueDisconnect(code);
        }

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
            Peer.Disconnect();
        }

        public LiteNetLibTransport()
        {
            Client = new NetManager(this);

            Client.Start();
        }

        public static DisconnectCode InfoToDisconnectCode(DisconnectInfo info)
        {
            if (info.Reason == DisconnectReason.RemoteConnectionClose)
            {
                try
                {
                    var value = info.AdditionalData.GetByte();

                    var code = (DisconnectCode)value;

                    return code;
                }
                catch (Exception)
                {
                    return DisconnectCode.Unknown;
                }
            }

            switch (info.Reason)
            {
                case DisconnectReason.DisconnectPeerCalled:
                    return DisconnectCode.Normal;

                case DisconnectReason.ConnectionFailed:
                    return DisconnectCode.ConnectionFailed;

                case DisconnectReason.Timeout:
                    return DisconnectCode.Timeout;

                case DisconnectReason.HostUnreachable:
                    return DisconnectCode.ServerUnreachable;

                case DisconnectReason.NetworkUnreachable:
                    return DisconnectCode.NetworkUnreachable;

                case DisconnectReason.ConnectionRejected:
                    return DisconnectCode.Rejected;
            }

            return DisconnectCode.Unknown;
        }
    }
}