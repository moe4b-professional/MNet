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
#if UNITY_WEBGL && !UNITY_EDITOR
    using NativeWebSocket;

    public class WebSocketTransport : NetworkTransport
    {
        public WebSocket Socket { get; protected set; }

        public override bool IsConnected
        {
            get
            {
                if (Socket == null) return false;

                return Socket.State == WebSocketState.Open;
            }
        }

        public const int Port = Utility.Port;

        public override int CheckMTU(DeliveryMode mode) => Utility.CheckMTU(mode);

        public override void Connect(GameServerID server, RoomID room)
        {
            var url = $"ws://{server}:{Port}/{room}";

            Socket = new WebSocket(url);

            Socket.OnOpen += OpenCallback;
            Socket.OnMessage += RecievedMessageCallback;
            Socket.OnClose += CloseCallback;

            Socket.Connect();
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        void Process()
        {
            if (Socket == null) return;

            Socket.DispatchMessageQueue();
        }
#endif

        void OpenCallback() => InvokeConnect();

        void RecievedMessageCallback(byte[] data) => InvokeMessages(data, DeliveryMode.Reliable);

        void CloseCallback(WebSocketCloseCode closeCode)
        {
            var value = (ushort)closeCode;

            var code = Utility.Disconnect.ValueToCode(value);

            InvokeDisconnect(code);
        }

        public override void Send(byte[] raw, DeliveryMode mode) => Socket.Send(raw);

        public override void Close() => Socket.Close();

        public WebSocketTransport() : base()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            NetworkAPI.OnProcess += Process;
#endif
        }
    }

#else
    using WebSocketSharp;

    public class WebSocketTransport : NetworkTransport
    {
        public WebSocket Socket { get; protected set; }

        public override bool IsConnected
        {
            get
            {
                if (Socket == null) return false;

                return Socket.ReadyState == WebSocketState.Open;
            }
        }

        public const int Port = Utility.Port;

        public override int CheckMTU(DeliveryMode mode) => Utility.CheckMTU(mode);

        public override void Connect(GameServerID server, RoomID room)
        {
            var url = $"ws://{server}:{Port}/{room}";

            Socket = new WebSocket(url);

            Socket.OnOpen += OpenCallback;
            Socket.OnMessage += RecievedMessageCallback;
            Socket.OnClose += CloseCallback;

            Socket.ConnectAsync();
        }

        void OpenCallback(object sender, EventArgs args) => InvokeConnect();

        void RecievedMessageCallback(object sender, MessageEventArgs args) => InvokeMessages(args.RawData, DeliveryMode.Reliable);

        void CloseCallback(object sender, CloseEventArgs args)
        {
            var code = Utility.Disconnect.ValueToCode(args.Code);

            InvokeDisconnect(code);
        }

        public override void Send(byte[] raw, DeliveryMode mode) => Socket.Send(raw);

        public override void Close() => Socket.Close(CloseStatusCode.Normal);

        public WebSocketTransport() : base()
        {

        }
    }
#endif
}