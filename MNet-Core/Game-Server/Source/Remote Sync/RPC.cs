using System;
using System.Linq;
using System.Text;

using System.Threading.Tasks;

using System.Collections;
using System.Collections.Generic;

namespace MNet
{
    class RpcBuffer
    {
        public Dictionary<ID, Collection> Dictionary { get; protected set; }
        public record struct ID(NetworkBehaviourID behaviour, RpcID method);
        public class Collection
        {
            public HashSet<MessageBufferHandle> HashSet { get; protected set; }

            public void Add(MessageBufferHandle message)
            {
                HashSet.Add(message);
            }

            public bool Remove(MessageBufferHandle message)
            {
                return HashSet.Remove(message);
            }
            public int RemoveAll(Predicate<MessageBufferHandle> match)
            {
                return HashSet.RemoveWhere(match);
            }

            public bool Contains(MessageBufferHandle message) => HashSet.Contains(message);

            public void Clear()
            {
                HashSet.Clear();
            }

            public Collection()
            {
                HashSet = new();
            }
        }

        public HashSet<MessageBufferHandle> Hash { get; protected set; }

        public void Set<TRequest, TCommand>(ref TRequest request, ref TCommand command, RemoteBufferMode mode, Room.MessageBufferProperty buffer)
            where TRequest : IRpcRequest
            where TCommand : IRpcCommand
        {
            var id = new ID(request.Behaviour, request.Method);

            if (Dictionary.TryGetValue(id, out var collection) == false)
            {
                collection = new Collection();
                Dictionary.Add(id, collection);
            }

            if (mode == RemoteBufferMode.Last)
            {
                buffer.RemoveAll(collection.HashSet);

                Hash.RemoveWhere(collection.Contains);
                collection.Clear();
            }

            var handle = buffer.Add(command);
            collection.Add(handle);
            Hash.Add(handle);
        }

        public void Clear(Room.MessageBufferProperty buffer)
        {
            buffer.RemoveAll(Hash);

            Hash.Clear();
            Dictionary.Clear();
        }

        public RpcBuffer()
        {
            Dictionary = new();
            Hash = new();
        }
    }
}