using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    class RpcBuffer
    {
        public Dictionary<string, NetworkMessageCollection> Dictionary { get; protected set; }

        public delegate void BufferDelegate(NetworkMessage message);
        public delegate void UnBufferDelegate(NetworkMessageCollection collection);

        public static string RequestToID(BroadcastRpcRequest request) => $"{request.Behaviour}{request.Method}";

        public void Set(NetworkMessage message, BroadcastRpcRequest request, BufferDelegate buffer, UnBufferDelegate unbuffer)
        {
            if (request.BufferMode == RpcBufferMode.None) return;

            var id = RequestToID(request);

            if (Dictionary.TryGetValue(id, out var collection) == false)
            {
                collection = new NetworkMessageCollection();

                Dictionary.Add(id, collection);
            }

            buffer(message);

            if (request.BufferMode == RpcBufferMode.Last)
            {
                unbuffer(collection);

                collection.Clear();
            }

            collection.Add(message);
        }

        public void Clear(UnBufferDelegate unbuffer)
        {
            foreach (var collection in Dictionary.Values)
            {
                unbuffer(collection);

                collection.Clear();
            }

            Dictionary.Clear();
        }

        public RpcBuffer()
        {
            Dictionary = new Dictionary<string, NetworkMessageCollection>();
        }
    }
}