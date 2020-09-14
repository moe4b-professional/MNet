using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace Backend
{
    class NetworkClient
    {
        public NetworkClientInfo Info { get; protected set; }

        public NetworkClientID ID => Info.ID;

        public NetworkClientProfile Profile => Info.Profile;
        public string Name => Profile.Name;
        public AttributesCollection Attributes => Profile.Attributes;

        public IWebSocketSession Session { get; protected set; }
        public string WebsocketID => Session.ID;
        public bool IsConnected => Session.State == WebSocketState.Open;

        public List<NetworkEntity> Entities { get; protected set; }

        public bool IsReady { get; protected set; }
        public void Ready()
        {
            IsReady = true;
        }

        public NetworkClientInfo ReadInfo() => new NetworkClientInfo(ID, Profile);

        public override string ToString() => ID.ToString();

        public NetworkClient(NetworkClientInfo info, IWebSocketSession session)
        {
            this.Info = info;
            this.Session = session;

            Entities = new List<NetworkEntity>();
        }
    }
}