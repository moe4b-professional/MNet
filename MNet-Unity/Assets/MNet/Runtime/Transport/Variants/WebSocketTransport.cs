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

using Utility = MNet.NetworkTransportUtility.WebSocket;

namespace MNet
{
    public class WebSocketTransport : NetworkTransport
    {
        public GlobalWebSocket Socket { get; protected set; }

        public override bool IsConnected
        {
            get
            {
                if (Socket == null) return false;

                return Socket.IsConnected;
            }
        }

        public const int Port = Utility.Port;

        public override int CheckMTU(DeliveryMode mode) => Utility.CheckMTU();

        public override void Connect(GameServerID server, RoomID room)
        {
            var url = $"ws://{server}:{Port}/{room}";

            Socket = new GlobalWebSocket(url);

            Socket.OnConnect += ConnectCallback;
            Socket.OnMessage += RecievedMessageCallback;
            Socket.OnDisconnect += DisconnectCallback;

            Socket.Connect();
        }

        void ConnectCallback() => InvokeConnect();

        void RecievedMessageCallback(byte[] raw)
        {
            var segment = new ArraySegment<byte>(raw);

            InvokeMessages(segment, DeliveryMode.ReliableOrdered);
        }

        void DisconnectCallback(DisconnectCode code, string reason) => InvokeDisconnect(code);

        public override void Send(ArraySegment<byte> segment, DeliveryMode mode, byte channel)
        {
            var raw = segment.ToArray();

            Socket.Send(raw);
        }

        public override void Close() => Socket.Disconnect();

        public WebSocketTransport() : base()
        {

        }
    }
}