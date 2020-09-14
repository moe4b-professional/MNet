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

        public RpcBuffer RpcBuffer { get; protected set; }
        public RprBuffer RprCache { get; protected set; }

        public override string ToString() => ID.ToString();

        public NetworkEntity(NetworkClient owner, NetworkEntityID id, NetworkEntityType type)
        {
            SetOwner(owner);

            this.ID = id;

            this.Type = type;

            RpcBuffer = new RpcBuffer();
            RprCache = new RprBuffer();
        }
    }
}