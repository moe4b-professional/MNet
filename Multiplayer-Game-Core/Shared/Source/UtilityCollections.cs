using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Concurrent;

namespace Backend
{
    public class Glossary<TKey, TValue>
    {
        public Dictionary<TValue, TKey> Keys { get; protected set; }
        public Dictionary<TKey, TValue> Values { get; protected set; }

        public TValue this[TKey key] => Values[key];
        public TKey this[TValue value] => Keys[value];

        public virtual void Set(TKey key, TValue value)
        {
            Values[key] = value;
            Keys[value] = key;
        }

        public virtual bool TryGetKey(TValue value, out TKey key) => Keys.TryGetValue(value, out key);
        public virtual bool TryGetValue(TKey key, out TValue value) => Values.TryGetValue(key, out value);

        public virtual bool Contains(TKey key) => Values.ContainsKey(key);
        public virtual bool Contains(TValue value) => Keys.ContainsKey(value);

        public virtual void Remove(TKey key)
        {
            if (Values.TryGetValue(key, out var value) == false)
            {
                Values.Remove(key);
                return;
            }

            Keys.Remove(value);
        }
        public virtual void Remove(TValue value)
        {
            if (Keys.TryGetValue(value, out var key) == false)
            {
                Keys.Remove(value);
                return;
            }

            Values.Remove(key);
        }

        public Glossary()
        {
            Values = new Dictionary<TKey, TValue>();

            Keys = new Dictionary<TValue, TKey>();
        }
    }

    public class AutoKeyCollection<TKey>
    {
        protected HashSet<TKey> hash;

        protected ConcurrentQueue<TKey> vacant;

        protected TKey index;

        public void Increment() => index = Incrementor(index);

        public delegate TKey IncrementDelegate(TKey value);
        public IncrementDelegate Incrementor { get; protected set; }

        object SyncLock = new object();

        public TKey Reserve()
        {
            lock (SyncLock)
            {
                if (vacant.TryDequeue(out var key) == false)
                {
                    key = index;

                    Increment();
                }

                Add(key);

                return key;
            }
        }

        void Add(TKey key)
        {
            hash.Add(key);
        }

        public bool Free(TKey key)
        {
            lock (SyncLock)
            {
                if (Contains(key) == false) return false;

                Remove(key);
                vacant.Enqueue(key);
                return true;
            }
        }

        void Remove(TKey key)
        {
            hash.Remove(key);
        }

        public bool Contains(TKey key) => hash.Contains(key);

        public AutoKeyCollection(IncrementDelegate incrementor)
        {
            hash = new HashSet<TKey>();
            vacant = new ConcurrentQueue<TKey>();

            index = default;
            this.Incrementor = incrementor;
        }
    }

    public class AutoKeyDictionary<TKey, TValue>
    {
        public Dictionary<TKey, TValue> Dictionary { get; protected set; }

        public IReadOnlyCollection<TValue> Values => Dictionary.Values;

        public AutoKeyCollection<TKey> Keys { get; protected set; }

        public int Count => Dictionary.Count;

        public TValue this[TKey key] => Dictionary[key];

        public virtual TKey Reserve()
        {
            var code = Keys.Reserve();

            return code;
        }

        public virtual void Assign(TKey key, TValue value)
        {
            Dictionary[key] = value;
        }

        public virtual bool TryGetValue(TKey key, out TValue value) => Dictionary.TryGetValue(key, out value);

        public virtual bool Contains(TKey key) => Dictionary.ContainsKey(key);

        public virtual void Remove(TKey key)
        {
            Dictionary.Remove(key);

            Keys.Free(key);
        }

        public AutoKeyDictionary(AutoKeyCollection<TKey>.IncrementDelegate incrementor)
        {
            Dictionary = new Dictionary<TKey, TValue>();

            Keys = new AutoKeyCollection<TKey>(incrementor);
        }
    }
}