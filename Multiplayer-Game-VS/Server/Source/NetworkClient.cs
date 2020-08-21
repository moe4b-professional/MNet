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

        public ClientInfo Info { get; protected set; }

        public string Name => Info.Name;

        public NetworkClient(NetworkClientID id, ClientInfo info)
        {
            this.ID = id;

            this.Info = info;
        }
    }
}