using System;
using System.Net;
using System.Text;
using System.Collections.Generic;

using WebSocketSharp.Server;
using WebSocketSharp;
using System.Diagnostics;

namespace Backend
{
    class WebSocketTransport : NetworkTransport<WebSocketTransport, WebSocketTransportContext, WebSocketTransportClient, IWebSocketSession, string>
    {
        public WebSocketServer Server { get; protected set; }

        public override void Start()
        {
            Server.Start();
        }

        protected override WebSocketTransportContext Create(uint id)
        {
            var context = new WebSocketTransportContext(this, id);

            return context;
        }

        public WebSocketTransport(IPAddress address, int port) : base()
        {
            Server = new WebSocketServer(address, port);

            Server.KeepClean = false;
        }
    }

    class WebSocketTransportContext : NetworkTransportContext<WebSocketTransport, WebSocketTransportContext, WebSocketTransportClient, IWebSocketSession, string>
    {
        public WebSocketServer Server => Transport.Server;

        public string Path { get; protected set; }

        public WebSocketServiceHost Host { get; protected set; }
        public WebSocketSessionManager Sessions => Host.Sessions;

        public class Behaviour : WebSocketBehavior
        {
            public WebSocketTransportContext TransportContext { get; protected set; }
            public void Set(WebSocketTransportContext reference) => TransportContext = reference;

            public WebSocketTransportClient Client { get; protected set; }

            public IWebSocketSession Session { get; protected set; }

            protected override void OnOpen()
            {
                base.OnOpen();

                Session = Sessions[ID];

                Client = TransportContext.RegisterClient(Session);
            }

            protected override void OnMessage(MessageEventArgs args)
            {
                base.OnMessage(args);

                TransportContext.RegisterMessage(Client, args.RawData);
            }

            protected override void OnClose(CloseEventArgs args)
            {
                base.OnClose(args);

                TransportContext.UnregisterClient(Client);
            }
        }
        void InitBehaviour(Behaviour behaviour) => behaviour.Set(this);

        protected override WebSocketTransportClient CreateClient(NetworkClientID clientID, IWebSocketSession session)
        {
            var client = new WebSocketTransportClient(this, clientID, session);

            return client;
        }

        public override void Send(WebSocketTransportClient client, byte[] raw)
        {
            if (client.IsOpen == false) return;

            Sessions.SendTo(raw, client.InternalID);
        }

        public override void Broadcast(byte[] raw)
        {
            Sessions.Broadcast(raw);
        }

        public override void Disconnect(WebSocketTransportClient client)
        {
            Sessions.CloseSession(client.InternalID);
        }

        public override void Close()
        {
            base.Close();

            Server.RemoveWebSocketService(Path);
        }

        public WebSocketTransportContext(WebSocketTransport transport, uint id) : base(transport, id)
        {
            this.Transport = transport;

            Path = $"/{ID}";

            Server.AddWebSocketService<Behaviour>(Path, InitBehaviour);

            Host = Server.WebSocketServices[Path];
        }
    }

    class WebSocketTransportClient : NetworkTransportClient<WebSocketTransportContext, IWebSocketSession, string>
    {
        public override string InternalID => Connection.ID;

        public bool IsOpen => Connection.State == WebSocketState.Open;

        public WebSocketTransportClient(WebSocketTransportContext context, NetworkClientID clientID, IWebSocketSession session) : base(context, clientID, session)
        {

        }
    }
}