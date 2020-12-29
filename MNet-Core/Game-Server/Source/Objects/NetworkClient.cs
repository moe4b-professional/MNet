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

        public bool Ready { get; protected set; }
        public void SetReady()
        {
            Ready = true;
        }

        public NetworkClientInfo ReadInfo() => new NetworkClientInfo(ID, Profile);
        public static NetworkClientInfo ReadInfo(NetworkClient client) => client.ReadInfo();

        public MessageSendQueue SendQueue { get; protected set; }

        public RprCache RprCache { get; protected set; }

        public override string ToString() => ID.ToString();

        public NetworkClient(NetworkClientInfo info)
        {
            this.Info = info;

            Entities = new List<NetworkEntity>();

            SendQueue = new MessageSendQueue(RealtimeAPI.Transport.CheckMTU);

            RprCache = new RprCache();
        }

        //Static Utility
        public static bool IsReady(NetworkClient client) => client.Ready;
    }
}