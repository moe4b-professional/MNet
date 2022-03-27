using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            }
        }

        /// <summary>
        /// The Remaining Amount of Capacity
        /// </summary>
        public int Remaining => Capacity - Position;

        #region Assign
        public void Assign(ArraySegment<byte> segment)
        {
            data = segment.Array;
            Position = segment.Offset;
        }
        #endregion

        #region Copy
        public NetworkStream Copy(Stream stream) => Copy(stream, (int)(stream.Length));
        public NetworkStream Copy(Stream stream, int count)
        {
            if (count > Remaining) Fit(count);

            stream.Read(data, Position, count);

            return this;
        }
        #endregion

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
        public ArraySegment<byte> ToSegment() => ToSegment(0, Position);
        public ArraySegment<byte> ToSegment(int offset, int count) => new ArraySegment<byte>(data, offset, count);

        public Span<byte> ToSpan() => ToSpan(0, Position);
        public Span<byte> ToSpan(int offset, int count) => new Span<byte>(data, offset, count);

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

        public void Insert(Span<byte> span) => Insert(span, 0, span.Length);
        public void Insert(Span<byte> span, int offset, int count)
        {
            if (count > Remaining) Fit(count);

            for (int i = offset; i < count; i++)
            {
                data[Position] = span[i];
                Position += 1;
            }
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
        public byte TakeByte()
        {
            Position += 1;

            return data[Position - 1];
        }

        /// <summary>
        /// Retrieves the Next Bytes in Memory and Iterates the Position by the Length
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public byte[] TakeArray(int length)
        {
            var raw = ToArray(Position, length);
            Position += length;

            return raw;
        }

        /// <summary>
        /// Retrieves the Next Bytes in Memory and Iterates the Position by the Length
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public Span<byte> TakeSpan(int length)
        {
            var span = new Span<byte>(data, Position, length);

            Position += length;

            return span;
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

            if (resolver == null)
            {
                NetworkSerializationResolver.Retrive(typeof(T));

                resolver = NetworkSerializationExplicitResolver<T>.Instance;

                if (resolver == null)
                    return false;
            }

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

            if (resolver == null)
            {
                NetworkSerializationResolver.Retrive(typeof(T));

                resolver = NetworkSerializationExplicitResolver<T>.Instance;

                if (resolver == null)
                    return false;
            }

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
        /// Recycles the Stream
        /// </summary>
        public void Dispose() => Reset();

        private NetworkStream() : this(null) { }
        private NetworkStream(int capacity) : this(new byte[capacity]) { }
        private NetworkStream(byte[] data) : this(data, 0) { }
        private NetworkStream(byte[] data, int position)
        {
            this.data = data;
            this.Position = position;
        }

        //Static Utility

        public static NetworkStream From(ArraySegment<byte> segment) => new NetworkStream(segment.Array, segment.Offset);

        public static class Pool
        {
            public static class Writer
            {
                static Queue<NetworkStream> Queue;

                public static int Count
                {
                    get
                    {
                        lock (Queue)
                        {
                            return Queue.Count;
                        }
                    }
                }

                public static int Allocations { get; private set; } = 0;

                public const int DefaultBinarySize = 1024;

                public ref struct Handle
                {
                    NetworkStream stream;

                    public void Dispose() => Return(stream);

                    public Handle(NetworkStream stream)
                    {
                        this.stream = stream;
                    }
                }

                public static Handle Lease(out NetworkStream stream)
                {
                    stream = Take();
                    return new Handle(stream);
                }
                public static NetworkStream Take()
                {
                    lock (Queue)
                    {
                        if (Queue.Count > 0)
                        {
                            return Queue.Dequeue();
                        }
                    }

                    return Create();
                }

                static NetworkStream Create()
                {
                    lock (Queue)
                    {
                        Allocations += 1;

#if DEBUG
                        if (Allocations % 100 == 0)
                            Log.Warning($"{Allocations} NetworkStreams Allocated, Are you Making Sure to Dispose of Old Network Streams?");
#endif
                    }

                    var stream = new NetworkStream(DefaultBinarySize);

                    return stream;
                }

                public static void Return(NetworkStream stream)
                {
                    stream.Reset();

                    lock (Queue)
                        Queue.Enqueue(stream);
                }

                static Writer()
                {
                    Queue = new Queue<NetworkStream>();
                }
            }

            public static class Reader
            {
                static Queue<NetworkStream> Queue;

                public static int Count
                {
                    get
                    {
                        lock (Queue)
                        {
                            return Queue.Count;
                        }
                    }
                }

                public static int Allocations { get; private set; } = 0;

                public ref struct Handle
                {
                    NetworkStream stream;

                    public void Dispose() => Return(stream);

                    public Handle(NetworkStream stream)
                    {
                        this.stream = stream;
                    }
                }

                public static Handle Lease(out NetworkStream stream)
                {
                    stream = Take();
                    return new Handle(stream);
                }
                public static NetworkStream Take()
                {
                    lock (Queue)
                    {
                        if (Queue.Count > 0)
                        {
                            return Queue.Dequeue();
                        }
                    }

                    return Create();
                }

                static NetworkStream Create()
                {
                    lock (Queue)
                    {
                        Allocations += 1;

#if DEBUG
                        if (Allocations % 100 == 0)
                            Log.Warning($"{Allocations} NetworkStreams Allocated, Are you Making Sure to Dispose of Old Network Streams?");
#endif
                    }

                    var stream = new NetworkStream();

                    return stream;
                }

                public static void Return(NetworkStream stream)
                {
                    stream.Reset();
                    stream.data = null;

                    lock (Queue)
                        Queue.Enqueue(stream);
                }

                static Reader()
                {
                    Queue = new Queue<NetworkStream>();
                }
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