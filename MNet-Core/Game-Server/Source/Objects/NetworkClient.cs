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
        public NetworkClientID ID { get; protected set; }

        public NetworkClientProfile Profile { get; protected set; }

        public string Name => Profile.Name;

        public AttributesCollection Attributes => Profile.Attributes;

        public List<NetworkEntity> Entities { get; protected set; }

        public NetworkClientInfo ReadInfo() => new NetworkClientInfo(ID, Profile);
        public static NetworkClientInfo ReadInfo(NetworkClient client) => client.ReadInfo();

        public MessageSendQueue SendQueue { get; protected set; }

        public override string ToString() => ID.ToString();

        public NetworkClient(NetworkClientID id, NetworkClientProfile profile, INetworkTransport transport)
        {
            this.ID = id;
            this.Profile = profile;

            Entities = new List<NetworkEntity>();

            SendQueue = new MessageSendQueue(transport.CheckMTU);
        }
    }
}