using System;
using System.Text;
using System.Collections.Generic;

using System.IO;

using System.Threading;

using System.Net;

using Utility = MNet.NetworkTransportUtility.WebSocket;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace MNet
{
    class WebSocketTransport : NetworkTransport<WebSocketTransport, WebSocketTransportContext, WebSocketTransportClient, WebSocket>
    {
        public WebSocketServer Server { get; }

        public const ushort Port = Utility.Port;

        public override int CheckMTU(DeliveryMode mode) => Utility.CheckMTU();

        public override void Start()
        {
            Server.Start();
        }

        #region Callbacks
        void ConnectCallback(WebSocket socket)
        {
            var key = socket.URL.TrimStart('/');

            if (RoomID.TryParse(key, out var room) == false)
            {
                var value = Utility.Disconnect.CodeToValue(DisconnectCode.ConnectionRejected);
                var code = (WebSocketCloseCode)value;

                socket.Disconnect(code);
                return;
            }
            if (Contexts.TryGetValue(room.Value, out var context) == false)
            {
                var value = Utility.Disconnect.CodeToValue(DisconnectCode.ConnectionRejected);
                var code = (WebSocketCloseCode)value;

                socket.Disconnect(code);
                return;
            }

            Thread.Sleep(200);

            var tag = new WebSocketClientTag();
            socket.Tag = tag;

            tag.Context = context;
            tag.Client = context.RegisterClient(socket);
        }
        void MessageCallback(WebSocket socket, WebSocketPacket packet)
        {
            WebSocketClientTag.Retrieve(socket, out var context, out var client);

            var segment = packet.AsSegment();

            context.InvokeMessage(client, segment, DeliveryMode.ReliableOrdered, 0, packet.Recycle);
        }
        void DisconnectCallback(WebSocket socket, WebSocketCloseCode code, string message)
        {
            WebSocketClientTag.Retrieve(socket, out var context, out var client);

            context.UnregisterClient(client);
        }
        #endregion

        public override void Stop(DisconnectCode code)
        {
            var value = (WebSocketCloseCode)Utility.Disconnect.CodeToValue(code);

            Server.Stop(value);
        }
        protected override void Close() { }

        public WebSocketTransport() : base()
        {
            Server = new WebSocketServer(IPAddress.Any, Port);

            Server.NoDelay = true;
            Server.PollingInterval = 1;

            Server.OnConnect += ConnectCallback;
            Server.OnMessage += MessageCallback;
            Server.OnDisconnect += DisconnectCallback;
        }
    }

    class WebSocketTransportContext : NetworkTransportContext<WebSocketTransport, WebSocketTransportContext, WebSocketTransportClient, WebSocket>
    {
        public override void Send(WebSocketTransportClient client, ArraySegment<byte> segment, DeliveryMode mode, byte channel)
        {
            if (client.IsOpen == false) return;

            segment = segment.ToArray();

            client.Connection.SendBinary(segment.AsSpan());
        }

        public override void Disconnect(WebSocketTransportClient client, DisconnectCode code)
        {
            if (client.IsOpen == false)
                return;

            var value = (WebSocketCloseCode)Utility.Disconnect.CodeToValue(code);

            client.Connection.Disconnect(value);
        }

        protected override void Close() { }
    }

    class WebSocketTransportClient : NetworkTransportClient<WebSocketTransportContext, WebSocket>
    {
        public bool IsOpen => Connection.State == WebSocketState.Open;
    }

    class WebSocketClientTag
    {
        public WebSocketTransportContext Context;
        public WebSocketTransportClient Client;

        public static WebSocketClientTag Retrieve(WebSocket socket) => socket.Tag as WebSocketClientTag;
        public static WebSocketClientTag Retrieve(WebSocket socket, out WebSocketTransportContext context, out WebSocketTransportClient client)
        {
            var tag = Retrieve(socket);

            context = tag.Context;
            client = tag.Client;

            return tag;
        }
    }
}