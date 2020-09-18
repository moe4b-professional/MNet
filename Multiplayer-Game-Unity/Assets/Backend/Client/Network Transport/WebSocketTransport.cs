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

using WebSocketSharp;
using System.Net;

namespace Backend
{
    public class WebSocketTransport : NetworkTransport
    {
        public WebSocket Socket { get; protected set; }

        public string Address { get; protected set; }
        public int Port { get; protected set; }

        public override bool IsConnected
        {
            get
            {
                if (Socket == null) return false;

                return Socket.ReadyState == WebSocketState.Open;
            }
        }

        public override void Connect(uint context)
        {
            var url = $"ws://{Address}:{Port}/{context}";

            Socket = new WebSocket(url);

            Socket.OnOpen += OpenCallback;
            Socket.OnMessage += RecievedMessageCallback;
            Socket.OnClose += CloseCallback;

            Socket.ConnectAsync();
        }

        #region Callbacks
        void OpenCallback(object sender, EventArgs args)
        {
            QueueConnect();
        }
        void RecievedMessageCallback(object sender, MessageEventArgs args)
        {
            var message = NetworkMessage.Read(args.RawData);

            QueueRecievedMessage(message);
        }
        void CloseCallback(object sender, CloseEventArgs args)
        {
            QueueDisconnect();
        }
        #endregion

        public override void Send(byte[] raw)
        {
            Socket.Send(raw);
        }

        public override void Close()
        {
            Socket.CloseAsync();
        }

        public WebSocketTransport(string address, int port) : base()
        {
            this.Address = address;
            this.Port = port;
        }
	}
}