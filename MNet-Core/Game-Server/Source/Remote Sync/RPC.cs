using System;
using System.Linq;
using System.Text;

using System.Threading.Tasks;

using System.Collections;
using System.Collections.Generic;

namespace MNet
{
    class RpcBuffer
    {
        public Dictionary<(NetworkBehaviourID behaviour, RpcID method), NetworkMessageCollection> Dictionary { get; protected set; }

        public HashSet<BufferNetworkMessage> Hash { get; protected set; }

        public delegate void BufferDelegate(BufferNetworkMessage message);
        public delegate void UnBufferAllDelegate(HashSet<BufferNetworkMessage> collection);

        public void Set<T>(BufferNetworkMessage message, ref T request, RemoteBufferMode mode, BufferDelegate buffer, UnBufferAllDelegate unbuffer)
            where T: IRpcRequest
        {
            var key = (request.Behaviour, request.Method);

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
            Dictionary = new Dictionary<(NetworkBehaviourID, RpcID), NetworkMessageCollection>();

            Hash = new HashSet<BufferNetworkMessage>();
        }
    }
}