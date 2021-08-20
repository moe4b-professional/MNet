using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// The Available Binary Capacity
        /// </summary>
        public int Capacity => data == null ? 0 : data.Length;

        int internal_position;
        /// <summary>
        /// The Current Position of the Stream
        /// </summary>
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

        /// <summary>
        /// The Remaining Amount of Capacity
        /// </summary>
        public int Remaining { get; protected set; }

        #region Sizing
        public const uint DefaultResizeLength = 512;

        /// <summary>
        /// Resize stream to fit a certian capacity
        /// </summary>
        /// <param name="capacity"></param>
        protected void Fit(int capacity)
        {
            if (capacity <= 0) throw new Exception($"Cannot Resize Network Buffer to Fit {capacity}");

            uint extra = DefaultResizeLength;

            while (capacity > Remaining + extra)
                extra += DefaultResizeLength;

            Resize(extra);
        }

        /// <summary>
        /// Adds Extra Capacity to Stream
        /// </summary>
        /// <param name="extra"></param>
        protected void Resize(uint extra)
        {
            var value = new byte[Capacity + extra];

            Buffer.BlockCopy(data, 0, value, 0, Position);

            this.data = value;
        }
        #endregion

        #region Slicing
        /// <summary>
        /// Returns an Array Segment Representing the Current State of the Stream
        /// </summary>
        /// <returns></returns>
        public ArraySegment<byte> Segment() => Segment(0, Position);
        public ArraySegment<byte> Segment(int offset, int count)
        {
            return new ArraySegment<byte>(data, offset, count);
        }

        /// <summary>
        /// Clones the Stream to a Byte Array
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray() => ToArray(0, Position);
        public byte[] ToArray(int offset, int count)
        {
            var destination = new byte[count];

            Buffer.BlockCopy(data, offset, destination, 0, count);

            return destination;
        }

        /// <summary>
        /// Clones the Stream to a Byte Array and Resets it
        /// </summary>
        /// <returns></returns>
        public byte[] Flush()
        {
            var raw = ToArray();

            Reset();

            return raw;
        }
        #endregion

        #region Insert
        public void Insert(byte value)
        {
            if (Remaining == 0) Resize(DefaultResizeLength);

            data[Position] = value;

            Position += 1;
        }

        public void Insert(ArraySegment<byte> segment) => Insert(segment.Array, segment.Offset, segment.Count);

        public void Insert(byte[] source) => Insert(source, 0, source.Length);
        public void Insert(byte[] source, int offset, int count)
        {
            if (count > Remaining) Fit(count);

            Buffer.BlockCopy(source, offset, data, Position, count);

            Position += count;
        }
        #endregion

        #region Take
        /// <summary>
        /// Retrieves the Next Byte in Stream and Iterates the Position by 1
        /// </summary>
        /// <returns></returns>
        public byte Take()
        {
            Position += 1;

            return data[Position - 1];
        }

        /// <summary>
        /// Retrieves the Next Bytes in Memory and Iterates the Position by the Length
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public byte[] Take(int length)
        {
            var raw = ToArray(Position, length);
            Position += length;

            return raw;
        }
        #endregion

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

        /// <summary>
        /// Resets the Stream State (Position)
        /// </summary>
        public void Reset()
        {
            Position = 0;
        }

        /// <summary>
        /// Was this Stream Leased from The Network Stream Pool?
        /// </summary>
        public bool IsLeased { get; private set; }

        /// <summary>
        /// Returns the Stream to The Pool if it was Leased from It
        /// </summary>
        public void Recycle()
        {
            if(IsLeased == false)
            {
                Log.Warning($"Current Network Stream is Not Leased from Pool, no Use Recycling It, Ignoring");
                return;
            }

            Pool.Return(this);
        }
        /// <summary>
        /// Recycles the Stream
        /// </summary>
        public void Dispose() => Recycle();

        public NetworkStream() : this(null) { }
        public NetworkStream(int capacity) : this(new byte[capacity]) { }
        public NetworkStream(byte[] data) : this(data, 0) { }
        public NetworkStream(byte[] data, int position)
        {
            this.data = data;
            this.Position = position;
        }

        //Static Utility

        public static class Pool
        {
            static Queue<NetworkStream> Queue;

            public static int Size
            {
                get
                {
                    lock (SyncLock)
                    {
                        return Queue.Count;
                    }
                }
            }

            static object SyncLock;

            public static NetworkStream Any => Lease();
            public static NetworkStream Lease()
            {
                lock (SyncLock)
                {
                    var stream = Queue.Count == 0 ? Create() : Queue.Dequeue();

                    return stream;
                }
            }

            public static int Allocations { get; private set; } = 0;

            static NetworkStream Create()
            {
                lock (SyncLock)
                {
                    Allocations += 1;

                    if (Allocations % 100 == 0)
                        Log.Warning($"{Allocations} NetworkStreams Allocated, Are you Making Sure to Recylce Old Network Streams?");
                }

                var stream = new NetworkStream(1024);
                stream.IsLeased = true;

                return stream;
            }

            public static void Return(NetworkStream stream)
            {
                stream.Reset();

                lock (SyncLock)
                {
                    Queue.Enqueue(stream);
                }
            }

            static Pool()
            {
                Queue = new Queue<NetworkStream>();

                SyncLock = new object();
            }
        }

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