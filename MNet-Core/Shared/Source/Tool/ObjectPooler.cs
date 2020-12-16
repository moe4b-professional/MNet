using System;
using System.Text;

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MNet
{
    public class ObjectPooler<T>
    {
        public ConcurrentQueue<T> Queue { get; protected set; }

        public CreateDelegate Create { get; protected set; }
        public delegate T CreateDelegate();

        public T Any => Lease();

        public T Lease()
        {
            if (Queue.TryDequeue(out var element)) return element;

            element = Create();

            Add(element);

            return element;
        }

        void Add(T element)
        {
            Queue.Enqueue(element);
        }

        public void Return(T element)
        {
            Add(element);
        }

        public ObjectPooler(CreateDelegate creator)
        {
            Queue = new ConcurrentQueue<T>();

            this.Create = creator;
        }
    }
}