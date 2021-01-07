using System;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;

namespace MNet
{
    class RpcBuffer
    {
        public Dictionary<(NetworkBehaviourID behaviour, RpxMethodID method), NetworkMessageCollection> Dictionary { get; protected set; }

        public HashSet<NetworkMessage> Hash { get; protected set; }

        public delegate void BufferDelegate(NetworkMessage message);
        public delegate void UnBufferAllDelegate(HashSet<NetworkMessage> collection);

        public void Set(NetworkMessage message, RpcType type, RemoteBufferMode mode, NetworkBehaviourID behaviour, RpxMethodID method, BufferDelegate buffer, UnBufferAllDelegate unbuffer)
        {
            if (type != RpcType.Broadcast)
            {
                Log.Error($"RPC of Type {type} isn't Supported for Buffering");
                return;
            }

            if (mode == RemoteBufferMode.None) return;

            var key = (behaviour, method);

            if (Dictionary.TryGetValue(key, out var collection) == false)
            {
                collection = new NetworkMessageCollection();

                Dictionary.Add(key, collection);
            }

            if (mode == RemoteBufferMode.Last)
            {
                unbuffer(collection.HashSet);

                Hash.RemoveWhere(collection.Contains);
                collection.Clear();
            }

            buffer(message);

            collection.Add(message);
            Hash.Add(message);
        }

        public void Clear(UnBufferAllDelegate unbuffer)
        {
            unbuffer(Hash);

            Hash.Clear();
            Dictionary.Clear();
        }

        public RpcBuffer()
        {
            Dictionary = new Dictionary<(NetworkBehaviourID, RpxMethodID), NetworkMessageCollection>();

            Hash = new HashSet<NetworkMessage>();
        }
    }
}