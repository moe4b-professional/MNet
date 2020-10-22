using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace MNet
{
    class NetworkClient
    {
        public NetworkClientInfo Info { get; protected set; }

        public NetworkClientID ID => Info.ID;

        public NetworkClientProfile Profile => Info.Profile;
        public string Name => Profile.Name;
        public AttributesCollection Attributes => Profile.Attributes;

        public List<NetworkEntity> Entities { get; protected set; }

        public bool IsReady { get; protected set; }
        public void Ready()
        {
            IsReady = true;
        }

        public NetworkClientInfo ReadInfo() => new NetworkClientInfo(ID, Profile);
        public static NetworkClientInfo ReadInfo(NetworkClient client) => client.ReadInfo();

        public override string ToString() => ID.ToString();

        public NetworkClient(NetworkClientInfo info)
        {
            this.Info = info;

            Entities = new List<NetworkEntity>();
        }
    }
}