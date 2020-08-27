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

        public delegate void UnBufferDelegate(IList<NetworkMessage> list);

        public void UnBufferAll(UnBufferDelegate action)
        {
            foreach (var collection in Dictionary.Values)
                action(collection);
        }

        public void Set(NetworkMessage message, RpcRequest request, UnBufferDelegate unbuffer)
        {
            if (request.BufferMode == RpcBufferMode.None) return;

            var id = RequestToID(request);

            if (Dictionary.TryGetValue(id, out var collection))
            {
                if (request.BufferMode == RpcBufferMode.Last)
                {
                    unbuffer(collection);
                    collection.Clear();
                }

                collection.Add(message);
            }
            else
            {
                collection = new NetworkMessageCollection();

                collection.Add(message);

                Dictionary.Add(id, collection);
            }
        }

        public static string RequestToID(RpcRequest request) => request.Entity + request.Method;

        public RpcBuffer()
        {
            Dictionary = new Dictionary<string, NetworkMessageCollection>();
        }
    }
}