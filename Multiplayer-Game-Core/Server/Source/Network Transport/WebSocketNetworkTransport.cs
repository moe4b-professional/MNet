using System;
using System.Net;
using System.Text;
using System.Collections.Generic;

using WebSocketSharp.Server;
using WebSocketSharp;
using System.Diagnostics;

namespace Backend
{
    class WebSocketNetworkTransport : NetworkTransport
    {
        public WebSocketServer Server { get; protected set; }

        public override void Start()
        {
            Server.Start();
        }

        protected override NetworkTransportContext Create(uint id)
        {
            var context = new WebSocketNetworkTransportContext(this, id);

            return context;
        }

        public WebSocketNetworkTransport(IPAddress address, int port) : base()
        {
            Server = new WebSocketServer(address, port);

            Server.KeepClean = false;
        }
    }

    class WebSocketNetworkTransportContext : NetworkTransportContext
    {
        public WebSocketNetworkTransport Transport { get; protected set; }
        public WebSocketServer Server => Transport.Server;

        public string Path { get; protected set; }

        public WebSocketServiceHost Host { get; protected set; }

        public Glossary<NetworkClientID, Client> WebSocketClients { get; protected set; }

        public class Client : WebSocketBehavior
        {
            public WebSocketNetworkTransportContext TransportContext { get; protected set; }

            public NetworkClientID ClientID { get; protected set; }

            public void Set(WebSocketNetworkTransportContext reference) => TransportContext = reference;

            public IWebSocketSession Session { get; protected set; }

            public bool IsOpen
            {
                get
                {
                    if (Session == null) return false;

                    return Session.State == WebSocketState.Open;
                }
            }

            protected override void OnOpen()
            {
                base.OnOpen();

                Session = Sessions[ID];

                ClientID = TransportContext.Reserve();

                TransportContext.OpenCallbacks(this);
            }

            protected override void OnMessage(MessageEventArgs args)
            {
                base.OnMessage(args);

                TransportContext.ReceivedMessageCallback(this, args.RawData);
            }

            protected override void OnClose(CloseEventArgs args)
            {
                base.OnClose(args);

                TransportContext.CloseCallback(this, args.Code, args.Reason);
            }
        }

        void InitClient(Client service) => service.Set(this);

        #region Internal Callbacks
        void OpenCallbacks(Client client)
        {
            WebSocketClients.Set(client.ClientID, client);

            QueueConnect(client.ClientID);
        }

        void ReceivedMessageCallback(Client client, byte[] raw)
        {
            var message = NetworkMessage.Read(raw);

            QueueRecievedMessage(client.ClientID, message, raw);
        }

        void CloseCallback(Client client, ushort code, string reason)
        {
            QueueDisconnect(client.ClientID);

            WebSocketClients.Remove(client.ClientID);
        }
        #endregion

        public override void Send(NetworkClientID target, byte[] raw)
        {
            if (WebSocketClients.TryGetValue(target, out var client) == false)
            {
                Log.Warning($"No WebSocketID Registered For NetworkClient {target}");
                return;
            }

            if (client.IsOpen == false) return;

            Host.Sessions.SendTo(raw, client.ID);
        }

        public override void Close()
        {
            Server.RemoveWebSocketService(Path);
        }

        public WebSocketNetworkTransportContext(WebSocketNetworkTransport transport, uint id) : base(id)
        {
            this.Transport = transport;

            Path = $"/{ID}";

            Server.AddWebSocketService<Client>(Path, InitClient);

            Host = Server.WebSocketServices[Path];

            WebSocketClients = new Glossary<NetworkClientID, Client>();
        }
    }
}