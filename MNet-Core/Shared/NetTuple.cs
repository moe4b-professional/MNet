using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace MNet
{
    [Preserve]
    public interface INetTuple
    {
        byte Length { get; }

        object this[int index] { get; }
    }

    [Preserve]
    public abstract class NetTuple : INetTuple, IEnumerable
    {
        public abstract byte Length { get; }

        public abstract object this[int index] { get; }

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
            }

            throw new ArgumentOutOfRangeException();
        }

        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < Length; i++)
                yield return this[i];
        }
    }

    [Preserve]
    public class NetTuple<T1> : NetTuple, INetTuple
    {
        public T1 Item1 { get; protected set; }

        public override byte Length => 1;

        public override object this[int index]
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

        public NetTuple() { }
        public NetTuple(T1 item1)
        {
            this.Item1 = item1;
        }
    }
    [Preserve]
    public class NetTuple<T1, T2> : NetTuple, INetTuple
    {
        public T1 Item1 { get; protected set; }
        public T2 Item2 { get; protected set; }

        public override byte Length => 2;

        public override object this[int index]
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

        public NetTuple() { }
        public NetTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
    [Preserve]
    public class NetTuple<T1, T2, T3> : NetTuple, INetTuple
    {
        public T1 Item1 { get; protected set; }
        public T2 Item2 { get; protected set; }
        public T3 Item3 { get; protected set; }

        public override byte Length => 3;

        public override object this[int index]
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

        public NetTuple() { }
        public NetTuple(T1 item1, T2 item2, T3 item3)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
        }
    }
    [Preserve]
    public class NetTuple<T1, T2, T3, T4> : NetTuple, INetTuple
    {
        public T1 Item1 { get; protected set; }
        public T2 Item2 { get; protected set; }
        public T3 Item3 { get; protected set; }
        public T4 Item4 { get; protected set; }

        public override byte Length => 4;

        public override object this[int index]
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

        public NetTuple() { }
        public NetTuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
        }
    }
    [Preserve]
    public class NetTuple<T1, T2, T3, T4, T5> : NetTuple, INetTuple
    {
        public T1 Item1 { get; protected set; }
        public T2 Item2 { get; protected set; }
        public T3 Item3 { get; protected set; }
        public T4 Item4 { get; protected set; }
        public T5 Item5 { get; protected set; }

        public override byte Length => 5;

        public override object this[int index]
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

        public NetTuple() { }
        public NetTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
            this.Item5 = item5;
        }
    }
}