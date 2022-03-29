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

using NativeWebSocket;

namespace MNet
{
    public class WebSocketTransport : NetworkTransport
    {
        public WebSocket Socket { get; protected set; }

        public override bool IsConnected
        {
            get
            {
                if (Socket == null)
                    return false;

                return Socket.State == WebSocketState.Open;
            }
        }

        public const int Port = Utility.Port;

        public override int CheckMTU(DeliveryMode mode) => Utility.CheckMTU();

        public override void Connect(GameServerID server, RoomID room)
        {
            var url = $"ws://{server}:{Port}/{room}";

            Socket = new WebSocket(url);

            Socket.OnOpen += ConnectCallback;
            Socket.OnMessage += RecievedMessageCallback;
            Socket.OnClose += DisconnectCallback;

            Socket.Connect().Forget();
        }

        public override void Send(ArraySegment<byte> segment, DeliveryMode mode, byte channel)
        {
            var raw = segment.ToArray();

            Socket.Send(raw);
        }

        public override void Disconnect(DisconnectCode code)
        {
            Socket.Close().Forget();
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        void Process()
        {
            if (IsConnected == false) return;

            Socket.DispatchMessageQueue();
        }
#endif

        #region Callbacks
        void ConnectCallback() => InvokeConnect();
        void RecievedMessageCallback(byte[] raw)
        {
            var segment = new ArraySegment<byte>(raw);

            InvokeMessage(segment, DeliveryMode.ReliableOrdered, null);
        }
        void DisconnectCallback(WebSocketCloseCode code)
        {
            var result = Utility.Disconnect.ValueToCode((ushort)code);

            InvokeDisconnect(result);
        }
        #endregion

        public WebSocketTransport() : base()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            NetworkAPI.OnProcess += Process;
#endif
        }
    }
}