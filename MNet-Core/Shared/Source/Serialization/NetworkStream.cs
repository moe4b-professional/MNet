using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MNet
{
    public class NetworkStream : IDisposable
    {
        protected byte[] data;
        public byte[] Data { get { return data; } }

        public void Set(byte[] data)
        {
            this.data = data;

            Reset();
        }

        public int Capacity => data == null ? 0 : data.Length;

        public int Size => Position;

        int internal_position;
        public int Position
        {
            get => internal_position;
            set
            {
                if (value < 0 || value > Capacity)
                    throw new IndexOutOfRangeException();

                internal_position = value;

                Remaining = Capacity - internal_position;
            }
        }

        public int Remaining { get; protected set; }

        #region Sizing
        public const uint DefaultResizeLength = 512;

        protected void Fit(int capacity)
        {
            if (capacity <= 0) throw new Exception($"Cannot Resize Network Buffer to Fit {capacity}");

            uint extra = DefaultResizeLength;

            while (capacity > Remaining + extra)
                extra += DefaultResizeLength;

            Resize(extra);
        }
        protected void Resize(uint extra)
        {
            var value = new byte[Capacity + extra];

            Buffer.BlockCopy(data, 0, value, 0, Position);

            this.data = value;
        }
        #endregion

        public byte[] ToArray()
        {
            var destination = new byte[Position];

            Buffer.BlockCopy(data, 0, destination, 0, destination.Length);

            return destination;
        }
        public byte[] Flush()
        {
            var raw = ToArray();

            Reset();

            return raw;
        }

        public void Insert(byte value)
        {
            if (Remaining == 0) Resize(DefaultResizeLength);

            data[Position] = value;

            Position += 1;
        }
        public void Insert(byte[] source) => Insert(source, source.Length);
        public void Insert(byte[] source, int count)
        {
            if (count > Remaining) Fit(count);

            Buffer.BlockCopy(source, 0, data, Position, count);

            Position += count;
        }

        public byte Pull()
        {
            Position += 1;

            return data[Position - 1];
        }
        public byte[] Pull(int length)
        {
            var destination = new byte[length];

            Buffer.BlockCopy(data, Position, destination, 0, length);

            Position += length;

            return destination;
        }

        #region Write
        public void Write<T>(T value)
        {
            if (ResolveExplicit(value)) return;

            var type = typeof(T);
            if (ResolveAny(value, type)) return;

            throw FormatResolverException<T>();
        }

        public void Write(object value)
        {
            if (value == null)
                throw new Exception("Cannot Serialize Null Without Explicilty Defining the Type Parameter");

            var type = value.GetType();

            Write(value, type);
        }
        public void Write(object value, Type type)
        {
            if (ResolveAny(value, type)) return;

            throw FormatResolverException(type);
        }

        #region Resolve
        bool ResolveExplicit<T>(T value)
        {
            var resolver = NetworkSerializationExplicitResolver<T>.Instance;

            if (resolver == null) return false;

            resolver.Serialize(this, value);
            return true;
        }

        bool ResolveAny(object value, Type type)
        {
            var resolver = NetworkSerializationResolver.Retrive(type);

            if (resolver == null) return false;

            resolver.Serialize(this, value, type);
            return true;
        }
        #endregion
        #endregion

        #region Read
        public void Read<T>(out T value) => value = Read<T>();
        public T Read<T>()
        {
            T value = default;

            if (ResolveExplicit(ref value)) return value;

            if (ResolveAny(ref value)) return value;

            throw FormatResolverException<T>();
        }

        public object Read(Type type)
        {
            if (ResolveAny(type, out var value)) return value;

            throw FormatResolverException(type);
        }

        #region Resolve
        bool ResolveExplicit<T>(ref T value)
        {
            var resolver = NetworkSerializationExplicitResolver<T>.Instance;
            if (resolver == null) return false;

            value = resolver.Deserialize(this);
            return true;
        }

        bool ResolveAny<T>(ref T value)
        {
            var type = typeof(T);

            if (ResolveAny(type, out var instance) == false)
                return false;

            try
            {
                value = (T)instance;
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"NetworkStream Trying to read {instance.GetType()} as {typeof(T)}");
            }

            return true;
        }
        bool ResolveAny(Type type, out object value)
        {
            var resolver = NetworkSerializationResolver.Retrive(type);
            if (resolver == null)
            {
                value = null;
                return false;
            }

            value = resolver.Deserialize(this, type);
            return true;
        }
        #endregion
        #endregion

        public void Reset()
        {
            Position = 0;
        }

        public bool IsLeased { get; private set; }
        public void Recycle()
        {
            if(IsLeased == false)
            {
                Log.Warning($"Current Network Stream is Not Leased from Pool, no Use Recycling It, Ignoring");
                return;
            }

            Pool.Return(this);
        }
        public void Dispose() => Recycle();

        public NetworkStream() : this(null) { }
        public NetworkStream(int capacity) : this(new byte[capacity]) { }
        public NetworkStream(byte[] data)
        {
            this.data = data;
            Position = 0;
        }

        //Static Utility

        public static class Pool
        {
            static Queue<NetworkStream> Queue;

            static object SyncLock;

            public static bool ThreadSafe { get; set; } = true;

            public static NetworkStream Any => Lease();

            public static NetworkStream Lease()
            {
                if (ThreadSafe) Monitor.Enter(SyncLock);

                var stream = Queue.Count == 0 ? Create() : Queue.Dequeue();

                if (ThreadSafe) Monitor.Exit(SyncLock);

                return stream;
            }

            static NetworkStream Create()
            {
                var stream = new NetworkStream(1024);

                stream.IsLeased = true;

                return stream;
            }

            public static void Return(NetworkStream stream)
            {
                stream.Reset();

                if (ThreadSafe) Monitor.Enter(SyncLock);

                Queue.Enqueue(stream);

                if (ThreadSafe) Monitor.Exit(SyncLock);
            }

            static Pool()
            {
                Queue = new Queue<NetworkStream>();

                SyncLock = new object();
            }
        }

        public static void Clear(NetworkStream stream) => stream.Reset();

        public static NotImplementedException FormatResolverException<T>()
        {
            var type = typeof(T);

            return FormatResolverException(type);
        }
        public static NotImplementedException FormatResolverException(Type type)
        {
            return new NotImplementedException($"Type {type} isn't supported for Network Serialization");
        }
    }
}