using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MNet
{
    public class RemoteConfig : INetworkSerializable
    {
        NetworkTransportType transport = default;
        public NetworkTransportType Transport => transport;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref transport);
        }

        public RemoteConfig() { }
        public RemoteConfig(NetworkTransportType transport)
        {
            this.transport = transport;
        }
    }
}