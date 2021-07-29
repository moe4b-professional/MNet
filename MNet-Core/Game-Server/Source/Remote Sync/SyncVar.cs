using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace MNet
{
    class SyncVarBuffer
    {
        public Dictionary<(NetworkBehaviourID behaviour, SyncVarID field), NetworkMessage> Dictionary { get; protected set; }

        public HashSet<NetworkMessage> Hash { get; protected set; }

        public delegate void BufferDelegate(NetworkMessage message);
        public delegate void UnBufferDelegate(NetworkMessage message);
        public delegate void UnBufferAllDelegate(HashSet<NetworkMessage> message);

        public void Set<T>(NetworkMessage message, ref T request, BufferDelegate buffer, UnBufferDelegate unbuffer)
            where T : ISyncVarRequest
        {
            var id = (request.Behaviour, request.Field);

            if (Dictionary.TryGetValue(id, out var previous))
            {
                unbuffer(previous);

                Hash.Remove(previous);
            }

            buffer(message);

            Dictionary[id] = message;
            Hash.Add(message);
        }

        public void Clear(UnBufferAllDelegate unbuffer)
        {
            unbuffer(Hash);

            Hash.Clear();
            Dictionary.Clear();
        }

        public SyncVarBuffer()
        {
            Dictionary = new Dictionary<(NetworkBehaviourID, SyncVarID), NetworkMessage>();

            Hash = new HashSet<NetworkMessage>();
        }
    }
}