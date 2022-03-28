using System;
using System.Text;

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MNet
{
    public static class ObjectPool<T>
        where T : class, new()
    {
        static ConcurrentQueue<T> Queue;

        public static int Size => Queue.Count;

        public static T Any => Lease();
        public static T Lease()
        {
            if (Queue.TryDequeue(out var element) == false)
                element = new T();

            return element;
        }

        public static void Return(T element)
        {
            Queue.Enqueue(element);
        }

        static ObjectPool()
        {
            Queue = new ConcurrentQueue<T>();
        }
    }
}