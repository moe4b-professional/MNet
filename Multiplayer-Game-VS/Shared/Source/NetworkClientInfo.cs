using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Shared
{
    [Serializable]
    public class NetworkClientInfo : INetworkSerializable
    {
        NetworkClientID id;
        public NetworkClientID ID => id;

        NetworkClientProfile profile;
        public NetworkClientProfile Profile => profile;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(id);
            writer.Write(profile);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out id);
            reader.Read(out profile);
        }

        public NetworkClientInfo() { }
        public NetworkClientInfo(NetworkClientID id, NetworkClientProfile profile)
        {
            this.id = id;

            this.profile = profile;
        }
    }
}