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

namespace MNet
{
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

        public override void Connect(GameServerID server, RoomID room)
        {
            var url = $"ws://{server}:{Port}/{room}";

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

            QueueRecievedMessage(message, DeliveryChannel.Reliable);
        }

        void CloseCallback(object sender, CloseEventArgs args)
        {
            var code = ValueToDisconnectCode(args.Code);

            QueueDisconnect(code);
        }
        #endregion

        public override void Send(byte[] raw, DeliveryChannel channel = DeliveryChannel.Reliable)
        {
            Socket.Send(raw);
        }

        public override void Close()
        {
            Socket.CloseAsync(CloseStatusCode.Normal);
        }

        public WebSocketTransport() : base()
        {

        }

        public static DisconnectCode ValueToDisconnectCode(ushort value)
        {
            if (value < Constants.NetworkTransport.WebSocket.DisconnectCodeOffset)
            {
                var code = (CloseStatusCode)value;

                switch (code)
                {
                    case CloseStatusCode.Normal:
                        return DisconnectCode.Normal;

                    case CloseStatusCode.InvalidData:
                        return DisconnectCode.InvalidData;
                }

                return DisconnectCode.Unknown;
            }

            value -= Constants.NetworkTransport.WebSocket.DisconnectCodeOffset;

            return (DisconnectCode)value;
        }
	}
}