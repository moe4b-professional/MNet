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

using System.Net;
using System.Threading;

using Ruffles;
using Ruffles.Core;
using Ruffles.Connections;
using Ruffles.Configuration;
using WebSocketSharp;

namespace Backend
{
    public class RufflesTransport : AutoDistributedNetworkTransport
    {
        public RuffleSocket Socket { get; protected set; }
        public Connection Connection { get; protected set; }

        public SocketConfig Config { get; protected set; }
        public IPEndPoint EndPoint { get; protected set; }

        public override bool IsConnected
        {
            get
            {
                if (IsRegistered == false) return false;

                if (Connection == null) return false;

                return Connection.State == ConnectionState.Connected;
            }
        }

        public override void Connect(uint context)
        {
            base.Connect(context);

            Socket.Start();
            Socket.Connect(EndPoint);
        }

        protected override void Tick()
        {
            if (Socket == null) return;
            if (Socket.IsRunning == false) return;

            var rEvent = Socket.Poll();
            RouteEvent(rEvent);
            rEvent.Recycle();
        }

        void RouteEvent(NetworkEvent rEvent)
        {
            switch (rEvent.Type)
            {
                case NetworkEventType.Nothing:
                    break;

                case NetworkEventType.Connect:
                    Connection = rEvent.Connection;
                    RequestRegister();
                    break;

                case NetworkEventType.Disconnect:
                    QueueDisconnect();
                    break;

                case NetworkEventType.Data:
                    var raw = rEvent.Data.ToArray();
                    ProcessMessage(raw);
                    break;
            }
        }

        public override void Send(byte[] raw) => Send(1, raw);
        protected virtual bool Send(byte channel, byte[] raw)
        {
            var segment = new ArraySegment<byte>(raw);

            return Connection.Send(segment, channel, false, 0);
        }

        public override void Close()
        {
            Socket.Stop();

            Stop();
        }

        public RufflesTransport(string address, ushort port)
        {
            Config = new SocketConfig()
            {
                ChallengeDifficulty = 20,
                DualListenPort = 0,
            };

            var ip = IPAddress.Parse(address);
            EndPoint = new IPEndPoint(ip, port);

            Socket = new RuffleSocket(Config);
        }
    }
}