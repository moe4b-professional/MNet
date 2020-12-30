﻿using System;
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

    class NetworkMessageBuffer
    {
        public List<NetworkMessage> List { get; protected set; }

        public bool TryGetIndex(NetworkMessage message, out int index)
        {
            for (index = 0; index < List.Count; index++)
            {
                if (Equals(List[index], message))
                    return true;
            }

            return false;
        }

        public void Set(int index, NetworkMessage message)
        {
            List[index] = message;
        }

        public void Add(NetworkMessage message)
        {
            List.Add(message);
        }

        public bool Remove(NetworkMessage message)
        {
            return List.Remove(message);
        }
        public int RemoveAll(Predicate<NetworkMessage> match)
        {
            return List.RemoveAll(match);
        }

        public NetworkMessageBuffer()
        {
            List = new List<NetworkMessage>();
        }
    }
}