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
        public Dictionary<(NetworkBehaviourID behaviour, RpcMethodID method), NetworkMessageCollection> Dictionary { get; protected set; }

        public HashSet<NetworkMessage> Hash { get; protected set; }

        public delegate void BufferDelegate(NetworkMessage message);
        public delegate void UnBufferAllDelegate(HashSet<NetworkMessage> collection);

        public void Set(NetworkMessage message, RpcType type, RemoteBufferMode mode, NetworkBehaviourID behaviour, RpcMethodID method, BufferDelegate buffer, UnBufferAllDelegate unbuffer)
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
            Dictionary = new Dictionary<(NetworkBehaviourID, RpcMethodID), NetworkMessageCollection>();

            Hash = new HashSet<NetworkMessage>();
        }
    }

    class RprCache
    {
        public List<RprPromise> Promises { get; protected set; }

        public int Count => Promises.Count;

        public RprPromise this[int index] => Promises[index];

        public void Register(NetworkClient requester, NetworkEntity entity, NetworkBehaviourID behaviour, RpcMethodID callback)
        {
            var promise = new RprPromise(requester, entity, behaviour, callback);

            Promises.Add(promise);
        }

        public bool Unregister(NetworkClient requester, NetworkEntity entity, NetworkBehaviourID behaviour, RpcMethodID callback)
        {
            for (int i = 0; i < Promises.Count; i++)
            {
                if (Promises[i].Equals(requester, entity, behaviour, callback))
                {
                    Promises.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public RprCache()
        {
            Promises = new List<RprPromise>();
        }
    }

    class RprPromise
    {
        public NetworkClient Requester { get; protected set; }

        public NetworkEntity Entity { get; protected set; }

        public NetworkBehaviourID Behaviour { get; protected set; }

        public RpcMethodID Callback { get; protected set; }

        public bool Equals(NetworkClient requester, NetworkEntity entity, NetworkBehaviourID behaviour, RpcMethodID callback)
        {
            if (this.Requester != requester) return false;
            if (this.Entity != entity) return false;
            if (this.Behaviour != behaviour) return false;
            if (this.Callback != callback) return false;

            return true;
        }

        public RprPromise(NetworkClient requester, NetworkEntity entity, NetworkBehaviourID behaviour, RpcMethodID callback)
        {
            this.Requester = requester;
            this.Entity = entity;
            this.Behaviour = behaviour;
            this.Callback = callback;
        }
    }
}