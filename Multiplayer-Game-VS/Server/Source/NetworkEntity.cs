using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Game.Shared;

namespace Game.Server
{
    class NetworkEntity
    {
        public NetworkEntityID ID { get; protected set; }

        public NetworkMessage SpawnMessage { get; set; }

        public List<NetworkMessage> RPCBuffer { get; protected set; }

        public NetworkEntity(NetworkEntityID id)
        {
            this.ID = id;

            this.RPCBuffer = new List<NetworkMessage>();
        }
    }
}