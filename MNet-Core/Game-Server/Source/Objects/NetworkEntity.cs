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

        public EntityType Type { get; internal set; }

        public bool IsSceneObject => Type == EntityType.SceneObject;
        public bool IsDynamic => Type == EntityType.Dynamic;
        public bool IsOrphan => Type == EntityType.Orphan;

        public bool IsMasterObject => CheckIfMasterObject(Type);

        public PersistanceFlags Persistance { get; protected set; }

        public Scene Scene { get; protected set; }

        public MessageBufferHandle<SpawnEntityCommand> SpawnCommand { get; set; }
        public MessageBufferHandle<object> OwnershipMessage { get; set; }

        public RpcBuffer RpcBuffer { get; protected set; }
        public SyncVarBuffer SyncVarBuffer { get; protected set; }

        public override string ToString() => ID.ToString();

        public NetworkEntity(NetworkClient owner, NetworkEntityID id, EntityType type, PersistanceFlags persistance, Scene scene)
        {
            SetOwner(owner);

            this.ID = id;

            this.Type = type;
            this.Persistance = persistance;

            this.Scene = scene;

            RpcBuffer = new RpcBuffer();
            SyncVarBuffer = new SyncVarBuffer();
        }

        //Static Utility
        public static bool CheckIfMasterObject(EntityType type) => type == EntityType.SceneObject || type == EntityType.Orphan;
    }
}