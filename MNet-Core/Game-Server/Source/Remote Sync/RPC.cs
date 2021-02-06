﻿using System;
using System.Linq;
using System.Text;

using System.Threading.Tasks;

using System.Collections;
using System.Collections.Generic;

namespace MNet
{
    class RpcBuffer
    {
        public Dictionary<(NetworkBehaviourID behaviour, RpcMethodID method), NetworkMessageCollection> Dictionary { get; protected set; }

        public HashSet<NetworkMessage> Hash { get; protected set; }

        public delegate void BufferDelegate(NetworkMessage message);
        public delegate void UnBufferAllDelegate(HashSet<NetworkMessage> collection);

        public void Set(NetworkMessage message, ref RpcBroadcastRequest request, BufferDelegate buffer, UnBufferAllDelegate unbuffer)
        {
            var key = (request.Behaviour, request.Method);

            if (Dictionary.TryGetValue(key, out var collection) == false)
            {
                collection = new NetworkMessageCollection();
                Dictionary.Add(key, collection);
            }

            if (request.BufferMode == RemoteBufferMode.Last)
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
}