using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace MNet
{
    class SyncVarBuffer
    {
        public Dictionary<ID, MessageBufferHandle> Dictionary { get; protected set; }

        public record struct ID(NetworkBehaviourID behaviour, SyncVarID field);

        public HashSet<MessageBufferHandle> Hash { get; protected set; }

        public void Set<TRequest, TCommand>(ref TRequest request, ref TCommand command, Room.MessageBufferProperty buffer)
            where TRequest : ISyncVarRequest
            where TCommand : ISyncVarCommand
        {
            var id = new ID(request.Behaviour, request.Field);

            if (Dictionary.TryGetValue(id, out var previous))
            {
                buffer.Remove(previous);
                Hash.Remove(previous);
            }

            var handle = buffer.Add(command);
            Dictionary[id] = handle;
            Hash.Add(handle);
        }

        public void Clear(Room.MessageBufferProperty buffer)
        {
            buffer.RemoveAll(Hash);

            Hash.Clear();
            Dictionary.Clear();
        }

        public SyncVarBuffer()
        {
            Dictionary = new();
            Hash = new();
        }
    }
}