using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    class NetworkMessageCollection
    {
        public HashSet<BufferNetworkMessage> HashSet { get; protected set; }

        public void Add(BufferNetworkMessage message)
        {
            HashSet.Add(message);
        }

        public bool Remove(BufferNetworkMessage message)
        {
            return HashSet.Remove(message);
        }
        public int RemoveAll(Predicate<BufferNetworkMessage> match)
        {
            return HashSet.RemoveWhere(match);
        }

        public bool Contains(BufferNetworkMessage message) => HashSet.Contains(message);

        public void Clear()
        {
            HashSet.Clear();
        }

        public NetworkMessageCollection()
        {
            HashSet = new HashSet<BufferNetworkMessage>();
        }
    }
}