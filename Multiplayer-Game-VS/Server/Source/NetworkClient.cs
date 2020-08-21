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

        public NetworkClientInfo Info { get; protected set; }

        public string Name => Info.Name;

        #region Entities
        public List<NetworkEntity> Entities { get; protected set; }

        public void RegisterEntity(NetworkEntity entity)
        {
            Entities.Add(entity);
        }

        public void RemoveEntity(NetworkEntity entity)
        {
            Entities.Remove(entity);
        }
        #endregion

        public NetworkClient(NetworkClientID id, NetworkClientInfo info)
        {
            this.ID = id;
            this.Info = info;

            Entities = new List<NetworkEntity>();
        }
    }
}