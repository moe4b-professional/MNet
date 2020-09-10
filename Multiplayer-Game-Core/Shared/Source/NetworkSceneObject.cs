using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    public class NetworkSceneObjectInfo : INetworkSerializable
    {
        int index;
        public int Index => index;

        NetworkEntityID id;
        public NetworkEntityID ID => id;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref index);
            context.Select(ref id);
        }

        public NetworkSceneObjectInfo() { }
        public NetworkSceneObjectInfo(int index, NetworkEntityID id)
        {
            this.index = index;
            this.id = id;
        }
    }
}