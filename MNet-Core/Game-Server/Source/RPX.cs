using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
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
        public Dictionary<ushort, RprCallback> Dictionary { get; protected set; }

        public IReadOnlyCollection<RprCallback> Collection => Dictionary.Values;

        public void Register(RpcRequest request, NetworkClient sender, NetworkClient target)
        {
            var callback = new RprCallback(request, sender, target);

            Dictionary.Add(request.Callback, callback);
        }

        public void TryGet(ushort callback, out RprCallback rpr) => Dictionary.TryGetValue(callback, out rpr);

        public bool Unregister(ushort callback)
        {
            return Dictionary.Remove(callback);
        }

        public void Clear() => Dictionary.Clear();

        public RprCache()
        {
            Dictionary = new Dictionary<ushort, RprCallback>();
        }
    }

    class RprCallback
    {
        public RpcRequest Request { get; protected set; }
        public ushort ID => Request.Callback;

        public NetworkClient Sender { get; protected set; }
        public NetworkClient Target { get; protected set; }

        public RprCallback(RpcRequest request, NetworkClient sender, NetworkClient target)
        {
            this.Request = request;

            this.Sender = sender;
            this.Target = target;
        }
    }
}