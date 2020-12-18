using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
    public class RemoteConfig : INetworkSerializable
    {
        NetworkTransportType transport;
        public NetworkTransportType Transport => transport;

        public void Select(ref NetworkSerializationContext context)
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