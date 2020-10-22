using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    class NetworkMessageCollection
    {
        public List<NetworkMessage> List { get; protected set; }

        public HashSet<NetworkMessage> HashSet { get; protected set; }

        public void Add(NetworkMessage message)
        {
            List.Add(message);

            HashSet.Add(message);
        }

        public bool Remove(NetworkMessage message)
        {
            HashSet.Remove(message);

            return List.Remove(message);
        }
        public int RemoveAll(Predicate<NetworkMessage> match)
        {
            HashSet.RemoveWhere(match);

            return List.RemoveAll(match);
        }

        public bool Contains(NetworkMessage message) => HashSet.Contains(message);

        public void Clear()
        {
            List.Clear();

            HashSet.Clear();
        }

        public NetworkMessageCollection()
        {
            List = new List<NetworkMessage>();
            HashSet = new HashSet<NetworkMessage>();
        }
    }
}