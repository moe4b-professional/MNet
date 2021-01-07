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

    class RprCache
    {
        public Dictionary<(NetworkClient client, RprChannelID channel), RprPromise> Promises { get; protected set; }

        public ICollection<RprPromise> Values => Promises.Values;

        public int Count => Promises.Count;

        public void Add(NetworkClient requester, RprChannelID channel)
        {
            var key = (requester, channel);

            var promise = new RprPromise(requester, channel);

            Promises.Add(key, promise);
        }

        public bool TryGet(NetworkClient requester, RprChannelID channel, out RprPromise promise)
        {
            var key = (requester, channel);

            return Promises.TryGetValue(key, out promise);
        }

        public bool Remove(NetworkClient requester, RprChannelID channel, out RprPromise promise)
        {
            var key = (requester, channel);

            return Promises.Remove(key, out promise);
        }

        public RprCache()
        {
            Promises = new Dictionary<(NetworkClient, RprChannelID), RprPromise>();
        }
    }

    struct RprPromise
    {
        public NetworkClient Requester { get; private set; }

        public RprChannelID Channel { get; private set; }

        public bool Equals(NetworkClient requester, RprChannelID channel)
        {
            if (this.Requester != requester) return false;
            if (this.Channel != channel) return false;

            return true;
        }

        public override int GetHashCode() => (Requester, Channel).GetHashCode();

        public RprPromise(NetworkClient requester, RprChannelID channel)
        {
            this.Requester = requester;
            this.Channel = channel;
        }
    }
}