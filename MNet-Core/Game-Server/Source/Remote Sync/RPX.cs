using System;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        public List<RprPromise> Promises { get; protected set; }

        public int Count => Promises.Count;

        public RprPromise this[int index] => Promises[index];

        public void Register(NetworkClient requester, RprChannelID channel)
        {
            var promise = new RprPromise(requester, channel);

            Promises.Add(promise);
        }

        public bool Unregister(NetworkClient requester, RprChannelID channel, out RprPromise promise)
        {
            for (int i = 0; i < Promises.Count; i++)
            {
                if (Promises[i].Equals(requester, channel))
                {
                    promise = Promises[i];
                    Promises.RemoveAt(i);
                    return true;
                }
            }

            promise = default;
            return false;
        }

        public RprCache()
        {
            Promises = new List<RprPromise>();
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