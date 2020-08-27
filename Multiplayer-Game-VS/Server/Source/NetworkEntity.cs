using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    class NetworkEntity
    {
        public NetworkEntityID ID { get; protected set; }

        public NetworkMessage SpawnMessage { get; set; }

        public RpcBuffer RPCBuffer { get; protected set; }

        public NetworkEntity(NetworkEntityID id)
        {
            this.ID = id;

            RPCBuffer = new RpcBuffer();
        }
    }
}