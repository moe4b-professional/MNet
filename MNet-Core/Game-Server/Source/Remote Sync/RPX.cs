using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    class RpcBuffer
    {
        public Dictionary<(NetworkBehaviourID behaviour, RpcMethodID method), NetworkMessageCollection> Dictionary { get; protected set; }

        public HashSet<NetworkMessage> Hash { get; protected set; }

        public delegate void BufferDelegate(NetworkMessage message);
        public delegate void UnBufferAllDelegate(HashSet<NetworkMessage> collection);

        public void Set(NetworkMessage message, RpcRequest request, BufferDelegate buffer, UnBufferAllDelegate unbuffer)
        {
            if (request.Type != RpcType.Broadcast)
            {
                Log.Error($"RPC {request} of Type {request.Type} isn't Supported for Buffering");
                return;
            }

            if (request.BufferMode == RpcBufferMode.None) return;

            var key = (request.Behaviour, request.Method);

            if (Dictionary.TryGetValue(key, out var collection) == false)
            {
                collection = new NetworkMessageCollection();

                Dictionary.Add(key, collection);
            }

            if (request.BufferMode == RpcBufferMode.Last)
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
            Dictionary = new Dictionary<(NetworkBehaviourID, RpcMethodID), NetworkMessageCollection>();

            Hash = new HashSet<NetworkMessage>();
        }
    }

    class RprCache
    {
        public Dictionary<(NetworkClientID client, ushort id), RprCallback> Dictionary { get; protected set; }

        public IReadOnlyCollection<RprCallback> Collection => Dictionary.Values;

        public void Register(RpcRequest request, NetworkClient sender, NetworkClient target)
        {
            var callback = new RprCallback(request, sender, target);

            var key = (sender.ID, request.Callback);

            Dictionary.Add(key, callback);
        }

        public bool TryGet(RprRequest request, out RprCallback callback) => TryGet(request.Target, request.ID, out callback);
        public bool TryGet(NetworkClientID client, ushort id, out RprCallback callback)
        {
            var key = (client, id);

            return Dictionary.TryGetValue(key, out callback);
        }

        public bool Unregister(RprRequest request) => Unregister(request.Target, request.ID);
        public bool Unregister(NetworkClientID client, ushort id)
        {
            var key = (client, id);

            return Dictionary.Remove(key);
        }

        public void Clear() => Dictionary.Clear();

        public RprCache()
        {
            Dictionary = new Dictionary<(NetworkClientID, ushort), RprCallback>();
        }
    }

    class RprCallback
    {
        public RpcRequest Request { get; protected set; }
        public ushort ID => Request.Callback;

        public NetworkClient Sender { get; protected set; }
        public NetworkClient Target { get; protected set; }

        public override string ToString() => $"[ ID: {ID} | Sender: {Sender} | Target: {Target} ]";

        public RprCallback(RpcRequest request, NetworkClient sender, NetworkClient target)
        {
            this.Request = request;

            this.Sender = sender;
            this.Target = target;
        }
    }
}