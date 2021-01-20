using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    class NetworkMessageCollection
    {
        public HashSet<NetworkMessage> HashSet { get; protected set; }

        public void Add(NetworkMessage message)
        {
            HashSet.Add(message);
        }

        public bool Remove(NetworkMessage message)
        {
            return HashSet.Remove(message);
        }
        public int RemoveAll(Predicate<NetworkMessage> match)
        {
            return HashSet.RemoveWhere(match);
        }

        public bool Contains(NetworkMessage message) => HashSet.Contains(message);

        public void Clear()
        {
            HashSet.Clear();
        }

        public NetworkMessageCollection()
        {
            HashSet = new HashSet<NetworkMessage>();
        }
    }
}