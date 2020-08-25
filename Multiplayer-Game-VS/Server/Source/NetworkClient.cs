using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Game.Shared;

using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace Game.Server
{
    class NetworkClient
    {
        public NetworkClientID ID { get; protected set; }

        public IWebSocketSession Session { get; protected set; }

        public NetworkClientProfile Profile { get; protected set; }

        public string Name => Profile.Name;

        public List<NetworkEntity> Entities { get; protected set; }

        public bool IsReady { get; protected set; }
        public void Ready()
        {
            IsReady = true;
        }

        public NetworkClientInfo ReadInfo() => new NetworkClientInfo(ID, Profile);

        public NetworkClient(NetworkClientID id, IWebSocketSession session, NetworkClientProfile profile)
        {
            this.ID = id;
            this.Session = session;
            this.Profile = profile;

            Entities = new List<NetworkEntity>();
        }
    }
}