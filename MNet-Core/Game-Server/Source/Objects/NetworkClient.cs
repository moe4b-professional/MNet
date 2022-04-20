using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MNet
{
    class NetworkClient
    {
        public NetworkClientID ID;

        public NetworkClientProfile Profile;

        public FixedString32 Name => Profile.Name;
        public AttributesCollection Attributes => Profile.Attributes;

        public HashSet<NetworkEntity> Entities;

        public HashSet<NetworkGroupID> Groups;

        public NetworkClientInfo ReadInfo() => new NetworkClientInfo(ID, Profile);
        public static NetworkClientInfo ReadInfo(NetworkClient client) => client.ReadInfo();

        public override string ToString() => ID.ToString();

        public NetworkClient(NetworkClientID id, NetworkClientProfile profile)
        {
            this.ID = id;
            this.Profile = profile;

            Entities = new HashSet<NetworkEntity>();

            Groups = new HashSet<NetworkGroupID>() { NetworkGroupID.Default };
        }
    }
}