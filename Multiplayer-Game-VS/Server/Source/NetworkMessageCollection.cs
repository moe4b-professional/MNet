using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Game.Shared;

namespace Game.Server
{
    class NetworkMessageCollection : List<NetworkMessage> //Yeah, I Know, But It's Easier Than Typing List<NetworkMessage> Everywhere!
    {
        public NetworkMessageCollection() : base() { }
    }
}