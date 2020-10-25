using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace MNet
{
    [Preserve]
    public interface INetTuple : IEnumerable
    {
        byte Length { get; }

        object this[int index] { get; }
    }

    [Preserve]
    public static class NetTuple
    {
        public static NetTuple<T1> Create<T1>(T1 item1)
            => new NetTuple<T1>(item1);
        public static NetTuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
            => new NetTuple<T1, T2>(item1, item2);
        public static NetTuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
            => new NetTuple<T1, T2, T3>(item1, item2, item3);
        public static NetTuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4)
            => new NetTuple<T1, T2, T3, T4>(item1, item2, item3, item4);
        public static NetTuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
            => new NetTuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
        public static NetTuple<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
            => new NetTuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);

        public static object Create(params object[] items)
        {
            var type = GetType(items.Length);

            return Create(type, items);
        }
        public static object Create(Type type, params object[] items)
        {
            var instance = Activator.CreateInstance(type, items);

            return instance;
        }

        public static Type GetType(int count)
        {
            switch (count)
            {
                case 1: return typeof(NetTuple<>);
                case 2: return typeof(NetTuple<,>);
                case 3: return typeof(NetTuple<,,>);
                case 4: return typeof(NetTuple<,,,>);
                case 5: return typeof(NetTuple<,,,,>);
                case 6: return typeof(NetTuple<,,,,,>);
            }

            throw new ArgumentOutOfRangeException();
        }
    }

    [Preserve]
    public struct NetTuple<T1> : INetTuple
    {
        public T1 Item1;

        public byte Length => 1;

        public object this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Item1;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public IEnumerator GetEnumerator() { for (int i = 0; i < Length; i++) yield return this[i]; }

        public NetTuple(T1 item1)
        {
            this.Item1 = item1;
        }

        public static implicit operator NetTuple<T1>(T1 value) => NetTuple.Create(value);

        public static implicit operator ValueTuple<T1>(NetTuple<T1> tuple) => ValueTuple.Create(tuple.Item1);
    }
    [Preserve]
    public struct NetTuple<T1, T2> : INetTuple
    {
        public T1 Item1;
        public T2 Item2;

        public byte Length => 2;

        public object this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Item1;
                    case 1: return Item2;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public NetTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public IEnumerator GetEnumerator() { for (int i = 0; i < Length; i++) yield return this[i]; }

        public static implicit operator NetTuple<T1, T2>((T1, T2) tuple)
        {
            return NetTuple.Create(tuple.Item1, tuple.Item2);
        }

        public static implicit operator (T1, T2)(NetTuple<T1, T2> tuple)
        {
            return ValueTuple.Create(tuple.Item1, tuple.Item2);
        }
    }
    [Preserve]
    public struct NetTuple<T1, T2, T3> : INetTuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;

        public byte Length => 3;

        public object this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Item1;
                    case 1: return Item2;
                    case 2: return Item3;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public IEnumerator GetEnumerator() { for (int i = 0; i < Length; i++) yield return this[i]; }

        public NetTuple(T1 item1, T2 item2, T3 item3)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
        }

        public static implicit operator NetTuple<T1, T2, T3>((T1, T2, T3) tuple)
        {
            return NetTuple.Create(tuple.Item1, tuple.Item2, tuple.Item3);
        }

        public static implicit operator (T1, T2, T3)(NetTuple<T1, T2, T3> tuple)
        {
            return ValueTuple.Create(tuple.Item1, tuple.Item2, tuple.Item3);
        }
    }
    [Preserve]
    public struct NetTuple<T1, T2, T3, T4> : INetTuple
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;

        public byte Length => 4;

        public object this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Item1;
                    case 1: return Item2;
                    case 2: return Item3;
                    case 3: return Item4;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public IEnumerator GetEnumerator() { for (int i = 0; i < Length; i++) yield return this[i]; }

        public NetTuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
        }

        public static implicit operator NetTuple<T1, T2, T3, T4>((T1, T2, T3, T4) tuple)
        {
            return NetTuple.Create(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
        }

        public static implicit operator (T1, T2, T3, T4)(NetTuple<T1, T2, T3, T4> tuple)
        {
            return ValueTuple.Create(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
        }
    }
    [Preserve]
    public struct NetTuple<T1, T2, T3, T4, T5>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;

        public byte Length => 5;

        public object this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Item1;
                    case 1: return Item2;
                    case 2: return Item3;
                    case 3: return Item4;
                    case 4: return Item5;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public IEnumerator GetEnumerator() { for (int i = 0; i < Length; i++) yield return this[i]; }

        public NetTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
            this.Item5 = item5;
        }

        public static implicit operator NetTuple<T1, T2, T3, T4, T5>((T1, T2, T3, T4, T5) tuple)
        {
            return NetTuple.Create(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5);
        }

        public static implicit operator (T1, T2, T3, T4, T5)(NetTuple<T1, T2, T3, T4, T5> tuple)
        {
            return ValueTuple.Create(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5);
        }
    }
    [Preserve]
    public struct NetTuple<T1, T2, T3, T4, T5, T6>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;

        public byte Length => 6;

        public object this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Item1;
                    case 1: return Item2;
                    case 2: return Item3;
                    case 3: return Item4;
                    case 4: return Item5;
                    case 5: return Item6;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public IEnumerator GetEnumerator() { for (int i = 0; i < Length; i++) yield return this[i]; }

        public NetTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
            this.Item5 = item5;
            this.Item6 = item6;
        }

        public static implicit operator NetTuple<T1, T2, T3, T4, T5, T6>((T1, T2, T3, T4, T5, T6) tuple)
        {
            return NetTuple.Create(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6);
        }

        public static implicit operator (T1, T2, T3, T4, T5, T6)(NetTuple<T1, T2, T3, T4, T5, T6> tuple)
        {
            return ValueTuple.Create(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6);
        }
    }
}