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
        public void Select(ref NetworkSerializationContext context)
        {
            
        }

        public RemoteConfig() { }
    }
}