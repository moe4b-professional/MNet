using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace MNet
{
    class SyncVarBuffer
    {
        public Dictionary<string, NetworkMessage> Dictionary { get; protected set; }

        public HashSet<NetworkMessage> Hash { get; protected set; }

        public delegate void BufferDelegate(NetworkMessage message);
        public delegate void UnBufferDelegate(NetworkMessage message);
        public delegate void UnBufferAllDelegate(HashSet<NetworkMessage> message);

        public static string RequestToID(SyncVarRequest request) => $"{request.Behaviour}{request.Variable}";

        public void Set(NetworkMessage message, SyncVarRequest request, BufferDelegate buffer, UnBufferDelegate unbuffer)
        {
            var id = RequestToID(request);

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
            Dictionary = new Dictionary<string, NetworkMessage>();

            Hash = new HashSet<NetworkMessage>();
        }
    }
}