using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    class Scene
    {
        internal byte Index;

        internal NetworkSceneLoadMode LoadMode;

        internal BufferNetworkMessage LoadMessage;

        internal HashSet<NetworkEntity> Entities;

        public Scene(byte index, NetworkSceneLoadMode loadMode, BufferNetworkMessage loadMessage)
        {
            this.Index = index;
            this.LoadMode = loadMode;
            this.LoadMessage = loadMessage;

            Entities = new HashSet<NetworkEntity>();
        }
    }
}