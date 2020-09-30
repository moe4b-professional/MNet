using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    class ActionQueue
    {
        public ConcurrentQueue<Callback> Collection { get; protected set; }

        public int Count => Collection.Count;

        public delegate void Callback();

        public void Enqueue(Callback callback) => Collection.Enqueue(callback);

        public bool Dequeue(out Callback callback) => Collection.TryDequeue(out callback);

        public ActionQueue()
        {
            Collection = new ConcurrentQueue<Callback>();
        }
    }
}