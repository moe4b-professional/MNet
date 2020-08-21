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

        public bool IsReady { get; protected set; }
        public void Ready()
        {
            IsReady = true;
        }

        public NetworkClientInfo ReadInfo() => new NetworkClientInfo(ID, Profile);

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

        public NetworkClient(NetworkClientID id, NetworkClientProfile profile)
        {
            this.ID = id;
            this.Profile = profile;

            Entities = new List<NetworkEntity>();
        }
    }
}