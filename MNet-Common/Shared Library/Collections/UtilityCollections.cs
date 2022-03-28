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
            where TKey : IEquatable<TKey>
    {
        TKey Index;
        TKey Max;

        HashSet<TKey> Hash;

        Queue<(DateTime stamp, TKey key)> Vacancies;

        public delegate TKey IncrementDelegate(TKey value);
        IncrementDelegate Incrementor;

        DateTime Timestamp => DateTime.Now;
        /// <summary>
        /// Time in Seconds untill a recycled key is valid for reuse
        /// </summary>
        public int RecycleTime { get; private set; }

        public TKey Reserve()
        {
            if (TryGetVacancy(out var key) == false)
                key = Increment();

            Register(key);

            return key;
        }
        public TKey Increment()
        {
            var key = Index;

            Index = Incrementor(Index);

            if (Index.Equals(Max))
                throw new OverflowException($"Auto Key Collection Index Overflow, Reached {typeof(TKey)} Max of {Max}");

            return key;
        }
        bool TryGetVacancy(out TKey key)
        {
            if (Vacancies.Count == 0)
            {
                key = default;
                return false;
            }

            var entry = Vacancies.Peek();

            if (Timestamp < entry.stamp)
            {
                key = default;
                return false;
            }

            Vacancies.Dequeue();

            key = entry.key;
            return true;
        }
        void Register(TKey key)
        {
            Hash.Add(key);
        }

        public bool Free(TKey key)
        {
            if (Contains(key) == false) return false;

            Unregister(key);

            var vacancy = (Timestamp.AddSeconds(RecycleTime), key);
            Vacancies.Enqueue(vacancy);

            return true;
        }
        void Unregister(TKey key)
        {
            Hash.Remove(key);
        }

        public bool Contains(TKey key) => Hash.Contains(key);

        public void Clear()
        {
            Index = default;
            Hash.Clear();
            Vacancies.Clear();
        }

        public AutoKeyCollection(TKey initial, TKey max, IncrementDelegate incrementor, int recycleTime)
        {
            Hash = new HashSet<TKey>();
            Vacancies = new Queue<(DateTime stamp, TKey key)>();

            this.Max = max;
            Index = initial;

            this.Incrementor = incrementor;

            this.RecycleTime = recycleTime;
        }
    }

    public class AutoKeyDictionary<TKey, TValue>
        where TKey : IEquatable<TKey>
    {
        public Dictionary<TKey, TValue> Dictionary { get; protected set; }

        public AutoKeyCollection<TKey> Keys { get; protected set; }

        public IReadOnlyCollection<TValue> Values => Dictionary.Values;

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

        public AutoKeyDictionary(TKey initial, TKey max, AutoKeyCollection<TKey>.IncrementDelegate incrementor, int recycleTime)
        {
            Dictionary = new Dictionary<TKey, TValue>();

            Keys = new AutoKeyCollection<TKey>(initial, max, incrementor, recycleTime);
        }
    }

    public class DualDictionary<TKey1, TKey2, TValue>
    {
        public Dictionary<TKey1, TValue> Dictionary1 { get; protected set; }
        public Dictionary<TKey2, TValue> Dictionary2 { get; protected set; }

        public ICollection<TKey1> Keys1 => Dictionary1.Keys;
        public ICollection<TKey2> Keys2 => Dictionary2.Keys;

        public ICollection<TValue> Values => Dictionary1.Values;

        public int Count => Dictionary1.Count;

        public TValue this[TKey1 key] => Dictionary1[key];
        public TValue this[TKey2 key] => Dictionary2[key];

        public TValue this[TKey1 key1, TKey2 key2]
        {
            set
            {
                Dictionary1[key1] = value;
                Dictionary2[key2] = value;
            }
        }

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