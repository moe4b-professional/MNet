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

using Backend;

using System.Net;
using System.Threading;

using Ruffles;
using Ruffles.Core;
using Ruffles.Connections;
using Ruffles.Configuration;
using WebSocketSharp;

namespace Game
{
    public class RufflesTransport : NetworkTransport
    {
        public RuffleSocket Socket { get; protected set; }
        public Connection Connection { get; protected set; }

        public SocketConfig Config { get; protected set; }
        public IPEndPoint EndPoint { get; protected set; }

        public bool IsRegistered { get; protected set; }

        public uint context;

        public override bool IsConnected
        {
            get
            {
                if (IsRegistered == false) return false;

                if (Connection == null) return false;

                return Connection.State == ConnectionState.Connected;
            }
        }

        Thread thread;
        public bool isRunning;

        public override void Connect(uint context)
        {
            this.context = context;

            IsRegistered = false;

            Application.quitting += ApplicationQuitCallback;

            Socket.Start();
            Socket.Connect(EndPoint);

            isRunning = true;
            thread = new Thread(Tick);
            thread.Start();
        }

        void Tick()
        {
            while (isRunning)
            {
                var rEvent = Socket.Poll();

                RouteEvent(rEvent);

                rEvent.Recycle();
            }
        }

        #region Events
        void RouteEvent(NetworkEvent rEvent)
        {
            switch (rEvent.Type)
            {
                case NetworkEventType.Nothing:
                    break;

                case NetworkEventType.Connect:
                    ConnectCallback(rEvent);
                    break;

                case NetworkEventType.Disconnect:
                    DisconnectCallback(rEvent);
                    break;

                case NetworkEventType.Data:
                    RecieveCallback(rEvent);
                    break;
            }
        }

        void ConnectCallback(NetworkEvent nEvent)
        {
            Connection = nEvent.Connection;

            RequestRegister();
        }
        void RecieveCallback(NetworkEvent nEvent)
        {
            var raw = nEvent.Data.ToArray();

            if (IsRegistered)
            {
                var message = NetworkMessage.Read(raw);

                QueueRecievedMessage(message);
            }
            else
            {
                RegisterCallback(raw);
            }
        }
        void DisconnectCallback(NetworkEvent nEvent)
        {
            QueueDisconnect();
        }
        #endregion

        void RequestRegister()
        {
            var raw = BitConverter.GetBytes(context);

            Send(raw);
        }
        void RegisterCallback(byte[] raw)
        {
            var code = raw[0];

            if (code == 200)
            {
                IsRegistered = true;
                QueueConnect();
            }
            else
            {
                QueueDisconnect();
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

        protected virtual void Stop()
        {
            isRunning = false;
        }

        void ApplicationQuitCallback()
        {
            Application.quitting -= ApplicationQuitCallback;

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