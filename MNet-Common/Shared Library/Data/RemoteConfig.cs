using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
    public struct RemoteConfig : INetworkSerializable
    {
        public void Select(ref NetworkSerializationContext context)
        {

        }
    }
}