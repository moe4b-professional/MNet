using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    class NetworkEntity
    {
        public NetworkClient Owner { get; protected set; }
        public void SetOwner(NetworkClient client)
        {
            Owner = client;
        }

        public NetworkEntityID ID { get; protected set; }

        public NetworkEntityType Type { get; internal set; }

        public bool IsSceneObject => Type == NetworkEntityType.SceneObject;
        public bool IsDynamic => Type == NetworkEntityType.Dynamic;
        public bool IsOrphan => Type == NetworkEntityType.Orphan;

        public bool IsMasterObject => CheckIfMasterObject(Type);

        public PersistanceFlags Persistance { get; protected set; }

        public NetworkMessage SpawnMessage { get; set; }

        public NetworkMessage? OwnershipMessage { get; set; }

        public RpcBuffer RpcBuffer { get; protected set; }
        public SyncVarBuffer SyncVarBuffer { get; protected set; }

        public override string ToString() => ID.ToString();

        public NetworkEntity(NetworkClient owner, NetworkEntityID id, NetworkEntityType type, PersistanceFlags persistance)
        {
            SetOwner(owner);

            this.ID = id;

            this.Type = type;
            this.Persistance = persistance;

            RpcBuffer = new RpcBuffer();
            SyncVarBuffer = new SyncVarBuffer();
        }

        //Static Utility
        public static bool CheckIfMasterObject(NetworkEntityType type) => type == NetworkEntityType.SceneObject || type == NetworkEntityType.Orphan;
    }
}