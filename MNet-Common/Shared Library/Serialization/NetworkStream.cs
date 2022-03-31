using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MNet
{
    public abstract class NetworkStream : IDisposable
    {
        protected byte[] data;
        public byte[] Data { get { return data; } }

        /// <summary>
        /// The Available Binary Capacity
        /// </summary>
        public virtual int Capacity => data.Length;

        protected int internal_position;
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

        /// <summary>
        /// Resets the Stream State (Position)
        /// </summary>
        public virtual void Reset()
        {
            Position = 0;
        }

        #region To Array
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

        /// <summary>
        /// Recycles the Stream
        /// </summary>
        public void Dispose() => Reset();

        protected NetworkStream(byte[] data, int position)
        {
            this.data = data;
            this.Position = position;
        }

        //Static Utility

        public static class Pool
        {
            public ref struct Handle
            {
                NetworkReader reader;
                NetworkWriter writer;

                public void Dispose()
                {
                    NetworkReader.Pool.Return(reader);
                    NetworkWriter.Pool.Return(writer);
                }

                public Handle(NetworkReader reader, NetworkWriter writer)
                {
                    this.reader = reader;
                    this.writer = writer;
                }
            }

            public static Handle Lease(out NetworkReader reader, out NetworkWriter writer)
            {
                Take(out reader, out writer);

                return new Handle(reader, writer);
            }
            public static void Take(out NetworkReader reader, out NetworkWriter writer)
            {
                reader = NetworkReader.Pool.Take();
                writer = NetworkWriter.Pool.Take();
            }

            public static void Return(NetworkReader reader, NetworkWriter writer)
            {
                NetworkReader.Pool.Return(reader);
                NetworkWriter.Pool.Return(writer);
            }
        }

        public static NotImplementedException FormatResolverException<T>()
        {
            var type = typeof(T);

            return FormatResolverException(type);
        }
        public static NotImplementedException FormatResolverException(Type type)
        {
            return new NotImplementedException($"Type ({type}) isn't supported for Network Serialization");
        }
    }

    public class NetworkReader : NetworkStream
    {
        int internal_capacity;
        public override int Capacity => internal_capacity;

        #region Assign
        public void Assign(byte[] array) => Assign(array, 0, array.Length);
        public void Assign(byte[] array, int offset, int count)
        {
            data = array;

            internal_capacity = count + offset;
            internal_position = offset;
        }

        public void Assign(ArraySegment<byte> segment) => Assign(segment.Array, segment.Offset, segment.Count);
        public void Assign(ByteChunk chunk) => Assign(chunk.Array, chunk.Offset, chunk.Count);
        public void Assign(NetworkWriter writer) => Assign(writer.Data, 0, writer.Position);
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

        /// <summary>
        /// Retrieves the Next Bytes in Memory and Iterates the Position by the Length
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public ByteChunk TakeChunk(int length)
        {
            var span = new ByteChunk(data, Position, length);

            Position += length;

            return span;
        }
        #endregion

        #region Read
        public T Read<T>()
        {
            T value = default;

            if (ResolveExplicit(ref value)) return value;

            if (ResolveAny(ref value)) return value;

            throw FormatResolverException<T>();
        }
        public void Read<T>(out T value) => value = Read<T>();

        public object Read(Type type)
        {
            if (ResolveAny(type, out var value)) return value;

            throw FormatResolverException(type);
        }
        #endregion

        #region Resolve
        bool ResolveExplicit<T>(ref T value)
        {
            var resolver = NetworkSerializationResolver.Retrieve<T>();

            if (resolver == null)
                return false;

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

        public override void Reset()
        {
            base.Reset();
            data = default;
            internal_capacity = 0;
        }

        protected NetworkReader() : base(null, 0) { }

        public new static class Pool
        {
            static ConcurrentQueue<NetworkReader> Queue;

            public static int Count => Queue.Count;

            public static int Allocations { get; private set; } = 0;

            public ref struct Handle
            {
                NetworkReader stream;

                public void Dispose() => Return(stream);

                public Handle(NetworkReader stream)
                {
                    this.stream = stream;
                }
            }

            public static Handle Lease(out NetworkReader stream)
            {
                stream = Take();
                return new Handle(stream);
            }
            public static NetworkReader Take()
            {
                if (Queue.TryDequeue(out var stream) == false)
                    stream = Create();

                return stream;
            }

            static NetworkReader Create()
            {
                lock (Queue)
                {
                    Allocations += 1;

#if DEBUG
                    if (Allocations % 100 == 0)
                        Log.Warning($"{Allocations} NetworkStreams Allocated, Are you Making Sure to Dispose of Old Network Streams?");
#endif
                }

                var stream = new NetworkReader();

                return stream;
            }

            public static void Return(NetworkReader stream)
            {
                stream.Reset();

                Queue.Enqueue(stream);
            }

            static Pool()
            {
                Queue = new ConcurrentQueue<NetworkReader>();
            }
        }
    }

    public class NetworkWriter : NetworkStream
    {
        #region Sizing
        public const uint DefaultResizeLength = 512;

        /// <summary>
        /// Resize stream to fit a certian capacity
        /// </summary>
        /// <param name="size"></param>
        protected void Fit(int size)
        {
            if (Remaining >= size) return;

            if (size <= 0) throw new Exception($"Cannot Resize Network Buffer to Fit {size}");

            uint extra = DefaultResizeLength;

            while (size > Remaining + extra)
                extra += DefaultResizeLength;

            Resize(extra);
        }

        /// <summary>
        /// Adds Extra Capacity to Stream
        /// </summary>
        /// <param name="extra"></param>
        protected void Resize(uint extra)
        {
            var destination = new byte[Capacity + extra];

            Buffer.BlockCopy(data, 0, destination, 0, Position);

            Pool.Buffer.Return(data);
            data = destination;
        }
        #endregion

        #region Slicing
        /// <summary>
        /// Returns an Array Segment Representing the Current State of the Stream
        /// </summary>
        /// <returns></returns>
        public ArraySegment<byte> AsSegment() => AsSegment(0, Position);
        public ArraySegment<byte> AsSegment(int offset, int count) => new ArraySegment<byte>(data, offset, count);

        public ByteChunk AsChunk() => AsChunk(0, Position);
        public ByteChunk AsChunk(int offset, int count) => new ByteChunk(Data, offset, count);

        public Span<byte> AsSpan() => AsSpan(0, Position);
        public Span<byte> AsSpan(int offset, int count) => new Span<byte>(data, offset, count);
        #endregion

        #region Insert
        public void Insert(byte value)
        {
            if (Remaining == 0) Resize(DefaultResizeLength);

            data[Position] = value;

            Position += 1;
        }

        public void Insert(byte[] source) => Insert(source, 0, source.Length);
        public void Insert(byte[] source, int offset, int count)
        {
            Fit(count);

            Buffer.BlockCopy(source, offset, data, Position, count);

            Position += count;
        }

        public void Insert(ArraySegment<byte> segment) => Insert(segment.Array, segment.Offset, segment.Count);
        public void Insert(ByteChunk chunk) => Insert(chunk.Array, chunk.Offset, chunk.Count);

        public void Insert(Span<byte> span)
        {
            Fit(span.Length);

            var destination = new Span<byte>(data, Position, span.Length);

            if (span.TryCopyTo(destination) == false)
                throw new InvalidOperationException("Couldn't Insert Span in Stream");

            Position += span.Length;
        }

        public void Insert(Stream stream) => Insert(stream, (int)(stream.Length));
        public void Insert(Stream stream, int count)
        {
            Fit(count);

            stream.Read(data, Position, count);

            Position += count;
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
        #endregion

        #region Resolve
        bool ResolveExplicit<T>(T value)
        {
            var resolver = NetworkSerializationResolver.Retrieve<T>();

            if (resolver == null)
                return false;

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

        protected NetworkWriter(byte[] data, int position) : base(data, position) { }

        public new static class Pool
        {
            public static class Buffer
            {
                static ConcurrentQueue<byte[]> Queue;

                public const int AllocationSize = 1024;

                public static byte[] Take()
                {
                    if (Queue.TryDequeue(out var buffer) == false)
                        buffer = new byte[AllocationSize];

                    return buffer;
                }

                public static void Return(byte[] buffer)
                {
                    Queue.Enqueue(buffer);
                }

                static Buffer()
                {
                    Queue = new ConcurrentQueue<byte[]>();
                }
            }

            static ConcurrentQueue<NetworkWriter> Queue;

            public static int Count => Queue.Count;

            public static int Allocations { get; private set; } = 0;

            public ref struct Handle
            {
                NetworkWriter stream;

                public void Dispose() => Return(stream);

                public Handle(NetworkWriter stream)
                {
                    this.stream = stream;
                }
            }

            public static Handle Lease(out NetworkWriter stream)
            {
                stream = Take();
                return new Handle(stream);
            }
            public static NetworkWriter Take()
            {
                if (Queue.TryDequeue(out var stream) == false)
                    stream = Create();

                return stream;
            }

            static NetworkWriter Create()
            {
                lock (Queue)
                {
                    Allocations += 1;

#if DEBUG
                    if (Allocations % 100 == 0)
                        Log.Warning($"{Allocations} NetworkStreams Allocated, Are you Making Sure to Dispose of Old Network Streams?");
#endif
                }

                var binary = Buffer.Take();
                var stream = new NetworkWriter(binary, 0);

                return stream;
            }

            public static void Return(NetworkWriter stream)
            {
                stream.Reset();

                Queue.Enqueue(stream);
            }

            static Pool()
            {
                Queue = new ConcurrentQueue<NetworkWriter>();
            }
        }
    }
}