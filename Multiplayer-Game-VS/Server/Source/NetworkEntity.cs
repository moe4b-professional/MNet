using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Game.Shared;

namespace Game.Server
{
    class NetworkEntity
    {
        public NetworkEntityID ID { get; protected set; }
        public void Configure(NetworkEntityID id)
        {
            this.ID = id;
        }

        public NetworkMessage SpawnMessage { get; set; }

        public NetworkEntityRPCBuffer RPCBuffer { get; protected set; }

        public NetworkEntity()
        {
            RPCBuffer = new NetworkEntityRPCBuffer();
        }
    }

    class NetworkEntityRPCBuffer
    {
        public Dictionary<string, EntityRpcCollection> Dictionary { get; protected set; }

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

            if(Dictionary.TryGetValue(id, out var collection))
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
                collection = new EntityRpcCollection();

                collection.Add(message);

                Dictionary.Add(id, collection);
            }
        }

        public static string RequestToID(RpcRequest request) => request.Entity + request.Method;

        public NetworkEntityRPCBuffer()
        {
            Dictionary = new Dictionary<string, EntityRpcCollection>();
        }
    }

    class EntityRpcCollection : List<NetworkMessage> //Yeah, I Know, But It's Easier Than Typing List<NetworkMessage> Everywhere!
    {
        public EntityRpcCollection() : base() { }
    }
}