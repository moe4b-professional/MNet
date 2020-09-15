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

        public HashSet<NetworkMessage> Hash { get; protected set; }

        public delegate void BufferDelegate(NetworkMessage message);
        public delegate void UnBufferAllDelegate(HashSet<NetworkMessage> collection);

        public static string RequestToID(RpcRequest request) => $"{request.Behaviour}{request.Method}";

        public void Set(NetworkMessage message, RpcRequest request, BufferDelegate buffer, UnBufferAllDelegate unbuffer)
        {
            if (request.Type != RpcType.Broadcast)
            {
                Log.Error($"RPC of Type {request.Type} isn't Supported for Buffering");
                return;
            }

            if (request.BufferMode == RpcBufferMode.None) return;

            var id = RequestToID(request);

            if (Dictionary.TryGetValue(id, out var collection) == false)
            {
                collection = new NetworkMessageCollection();

                Dictionary.Add(id, collection);
            }

            if (request.BufferMode == RpcBufferMode.Last)
            {
                unbuffer(collection.HashSet);

                collection.Clear();
                Hash.RemoveWhere(x => collection.Contains(x));
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
            Dictionary = new Dictionary<string, NetworkMessageCollection>();

            Hash = new HashSet<NetworkMessage>();
        }
    }

    class RprCache
    {
        public Dictionary<ushort, Data> Dictionary { get; protected set; }

        public IReadOnlyCollection<Data> Collection => Dictionary.Values;

        public class Data
        {
            public RpcRequest Request { get; protected set; }
            public ushort ID => Request.Callback;

            public NetworkClient Sender { get; protected set; }

            public Data(RpcRequest request, NetworkClient sender)
            {
                this.Request = request;
                this.Sender = sender;
            }
        }

        public void Register(RpcRequest request, NetworkClient sender)
        {
            var callback = new Data(request, sender);

            Dictionary.Add(request.Callback, callback);
        }

        public bool Unregister(ushort callback)
        {
            return Dictionary.Remove(callback);
        }

        public void Clear() => Dictionary.Clear();

        public RprCache()
        {
            Dictionary = new Dictionary<ushort, Data>();
        }
    }
}