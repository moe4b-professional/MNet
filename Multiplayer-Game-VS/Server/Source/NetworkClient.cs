using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Game.Shared;

namespace Game.Server
{
    class NetworkClient
    {
        public NetworkClientID ID { get; protected set; }

        public NetworkClientProfile Profile { get; protected set; }

        public string Name => Profile.Name;

        public List<NetworkEntity> Entities { get; protected set; }

        public bool IsReady { get; protected set; }
        public void Ready()
        {
            IsReady = true;
        }

        public NetworkMessage ConnectMessage { get; set; }

        public NetworkClient(NetworkClientID id, NetworkClientProfile profile)
        {
            this.ID = id;
            this.Profile = profile;

            Entities = new List<NetworkEntity>();
        }
    }
}