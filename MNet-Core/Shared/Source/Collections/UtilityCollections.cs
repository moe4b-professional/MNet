using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace MNet
{
    public class Glossary<TKey, TValue>
    {
        public Dictionary<TValue, TKey> Keys { get; protected set; }
        public Dictionary<TKey, TValue> Values { get; protected set; }

        public TValue this[TKey key] => Values[key];
        public TKey this[TValue value] => Keys[value];

        public virtual void Add(TKey key, TValue value)
        {
            Values.Add(key, value);
            Keys.Add(value, key);
        }

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

        protected Queue<TKey> vacant;

        protected TKey index;

        public TKey Increment()
        {
            var key = index;

            index = Incrementor(index);

            return key;
        }

        public delegate TKey IncrementDelegate(TKey value);
        public IncrementDelegate Incrementor { get; protected set; }

        public TKey Reserve()
        {
            if (vacant.Count > 0)
            {
                var key = vacant.Dequeue();

                Add(key);

                return key;
            }
            else
            {
                var key = Increment();

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
            if (Contains(key) == false) return false;

            Remove(key);
            vacant.Enqueue(key);
            return true;
        }

        public bool Contains(TKey key) => hash.Contains(key);

        void Remove(TKey key)
        {
            hash.Remove(key);
        }

        public void Clear()
        {
            index = default;
            hash.Clear();
            vacant.Clear();
        }

        public AutoKeyCollection(IncrementDelegate incrementor, TKey initial)
        {
            hash = new HashSet<TKey>();
            vacant = new Queue<TKey>();

            index = initial;
            this.Incrementor = incrementor;
        }
        public AutoKeyCollection(IncrementDelegate incrementor) : this(incrementor, default) { }
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

        public virtual bool Remove(TKey key)
        {
            var removed = Dictionary.Remove(key);

            Keys.Free(key);

            return removed;
        }

        public virtual void Clear()
        {
            Dictionary.Clear();
            Keys.Clear();
        }

        public AutoKeyDictionary(AutoKeyCollection<TKey>.IncrementDelegate incrementor, TKey initial)
        {
            Dictionary = new Dictionary<TKey, TValue>();

            Keys = new AutoKeyCollection<TKey>(incrementor, initial);
        }
        public AutoKeyDictionary(AutoKeyCollection<TKey>.IncrementDelegate incrementor) : this(incrementor, default) { }
    }

    public class DualDictionary<TKey1, TKey2, TValue>
    {
        public Dictionary<TKey1, TValue> Dictionary1 { get; protected set; }
        public Dictionary<TKey2, TValue> Dictionary2 { get; protected set; }

        public int Count => Dictionary1.Count;

        public TValue this[TKey1 key] => Dictionary1[key];
        public TValue this[TKey2 key] => Dictionary2[key];

        public void Add(TKey1 key1, TKey2 key2, TValue value)
        {
            Dictionary1.Add(key1, value);
            Dictionary2.Add(key2, value);
        }

        public bool Contains(TKey1 key) => Dictionary1.ContainsKey(key);
        public bool Contains(TKey2 key) => Dictionary2.ContainsKey(key);

        public bool TryGetValue(TKey1 key, out TValue value) => Dictionary1.TryGetValue(key, out value);
        public bool TryGetValue(TKey2 key, out TValue value) => Dictionary2.TryGetValue(key, out value);

        public bool Remove(TKey1 key1, TKey2 key2)
        {
            var removed = false;

            removed |= Dictionary1.Remove(key1);
            removed |= Dictionary2.Remove(key2);

            return removed;
        }

        public void Clear()
        {
            Dictionary1.Clear();
            Dictionary2.Clear();
        }

        public DualDictionary() : this(0) { }
        public DualDictionary(int capacity)
        {
            Dictionary1 = new Dictionary<TKey1, TValue>(capacity);
            Dictionary2 = new Dictionary<TKey2, TValue>(capacity);
        }
    }
}