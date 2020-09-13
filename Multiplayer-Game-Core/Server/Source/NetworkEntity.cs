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
        public void SetOwner(NetworkClient client)
        {
            this.Owner = client;
        }

        public NetworkEntityID ID { get; protected set; }

        public NetworkEntityType Type { get; protected set; }

        public NetworkMessage SpawnMessage { get; set; }
        public RpcBuffer RPCBuffer { get; protected set; }

        public NetworkEntity(NetworkClient owner, NetworkEntityID id, NetworkEntityType type)
        {
            SetOwner(owner);

            this.ID = id;

            this.Type = type;

            RPCBuffer = new RpcBuffer();
        }
    }
}