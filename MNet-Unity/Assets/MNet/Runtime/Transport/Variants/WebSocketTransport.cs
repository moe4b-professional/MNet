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

using Utility = MNet.NetworkTransportUtility.WebSocket;

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

        public const int Port = Constants.Server.Game.Realtime.Port;

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

        #region Callbacks
        void OpenCallback(object sender, EventArgs args)
        {
            Socket.DisableNagleAlgorithm();

            InvokeConnect();
        }

        void RecievedMessageCallback(object sender, MessageEventArgs args) => InvokeMessages(args.RawData, DeliveryMode.Reliable);

        void CloseCallback(object sender, CloseEventArgs args)
        {
            var code = Utility.Disconnect.ValueToCode(args.Code);

            InvokeDisconnect(code);
        }
        #endregion

        public override void Send(byte[] raw, DeliveryMode mode)
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
    }
}