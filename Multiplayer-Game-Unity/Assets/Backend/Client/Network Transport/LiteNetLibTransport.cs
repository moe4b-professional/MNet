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

namespace Backend
{
    public class LiteNetLibTransport : AutoDistributedNetworkTransport
    {
        public NetManager Client { get; protected set; }

        public NetPeer Peer { get; protected set; }

        public EventBasedNetListener Listener { get; protected set; }

        public string Address { get; protected set; }
        public int Port { get; protected set; }

        public override bool IsConnected
        {
            get
            {
                if (IsRegistered == false) return false;

                if (Peer == null) return false;

                return Peer.ConnectionState == ConnectionState.Connected;
            }
        }

        public override void Connect(uint context)
        {
            base.Connect(context);

            Client.Start();

            Peer = Client.Connect(Address, Port, "");
        }

        protected override void Tick()
        {
            if (Client == null) return;
            if (Client.IsRunning == false) return;

            Client.PollEvents();

            Thread.Sleep(15);
        }

        #region Callbacks
        void PeerConnectionCallback(NetPeer peer) => RequestRegister();

        void PeerDisconnectCallback(NetPeer peer, DisconnectInfo disconnectInfo) => QueueDisconnect();

        void RecieveCallback(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var raw = new byte[reader.AvailableBytes];
            Buffer.BlockCopy(reader.RawData, reader.Position, raw, 0, reader.AvailableBytes);

            ProcessMessage(raw);
        }
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

        public LiteNetLibTransport(string address, int port)
        {
            this.Address = address;
            this.Port = port;

            Listener = new EventBasedNetListener();

            Listener.PeerConnectedEvent += PeerConnectionCallback;
            Listener.PeerDisconnectedEvent += PeerDisconnectCallback;
            Listener.NetworkReceiveEvent += RecieveCallback;

            Client = new NetManager(Listener);
        }
    }
}