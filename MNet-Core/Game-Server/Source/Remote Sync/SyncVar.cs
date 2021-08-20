using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace MNet
{
    class SyncVarBuffer
    {
        public Dictionary<(NetworkBehaviourID behaviour, SyncVarID field), BufferNetworkMessage> Dictionary { get; protected set; }

        public HashSet<BufferNetworkMessage> Hash { get; protected set; }

        public delegate void BufferDelegate(BufferNetworkMessage message);
        public delegate void UnBufferDelegate(BufferNetworkMessage message);
        public delegate void UnBufferAllDelegate(HashSet<BufferNetworkMessage> message);

        public void Set<T>(BufferNetworkMessage message, ref T request, BufferDelegate buffer, UnBufferDelegate unbuffer)
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
            Dictionary = new Dictionary<(NetworkBehaviourID, SyncVarID), BufferNetworkMessage>();

            Hash = new HashSet<BufferNetworkMessage>();
        }
    }
}