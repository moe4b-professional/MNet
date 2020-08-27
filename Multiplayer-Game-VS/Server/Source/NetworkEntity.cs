using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    class NetworkEntity
    {
        public NetworkClient Owner { get; protected set; }

        public NetworkEntityID ID { get; protected set; }

        public NetworkMessage SpawnMessage { get; set; }

        public RpcBuffer RPCBuffer { get; protected set; }

        public NetworkEntity(NetworkClient owner, NetworkEntityID id)
        {
            this.Owner = owner;
            this.ID = id;

            RPCBuffer = new RpcBuffer();
        }
    }
}