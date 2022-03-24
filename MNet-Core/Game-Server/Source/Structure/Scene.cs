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

        internal MessageBufferHandle<LoadScenePayload> LoaPayload;

        internal HashSet<NetworkEntity> Entities;

        public Scene(byte index, NetworkSceneLoadMode loadMode, MessageBufferHandle<LoadScenePayload> loadPayload)
        {
            this.Index = index;
            this.LoadMode = loadMode;
            this.LoaPayload = loadPayload;

            Entities = new HashSet<NetworkEntity>();
        }
    }
}