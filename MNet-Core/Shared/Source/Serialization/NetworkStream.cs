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
            return new NotImplementedException($"Type {type} isn't supported for Network Serialization");
        }
    }

    public class NetworkReader : NetworkStream
    {
        #region Assign
        public void Assign(byte[] array) => Assign(array, 0);
        public void Assign(byte[] array, int offset)
        {
            data = array;
            Position = offset;
        }

        public void Assign(ArraySegment<byte> segment)
        {
            data = segment.Array;
            Position = segment.Offset;
        }

        public void Assign(ByteChunk segment)
        {
            data = segment.Array;
            Position = segment.Offset;
        }

        public void Assign(NetworkWriter writer) => Assign(writer.Data);
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

        public override void Reset()
        {
            base.Reset();
            data = default;
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
        /// <param name="capacity"></param>
        protected void Fit(int capacity)
        {
            if (Remaining >= capacity) return;

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

        #region Copy
        public void Copy(ByteChunk segment)
        {
            Fit(segment.Count);

            Buffer.BlockCopy(segment.Array, segment.Offset, data, Position, segment.Count);
        }

        public NetworkStream Copy(Stream stream) => Copy(stream, (int)(stream.Length));
        public NetworkStream Copy(Stream stream, int count)
        {
            Fit(count);

            stream.Read(data, Position, count);

            return this;
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

        public void Insert(Span<byte> span) => Insert(span, 0, span.Length);
        public void Insert(Span<byte> span, int offset, int count)
        {
            Fit(count);

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
            Fit(count);

            Buffer.BlockCopy(source, offset, data, Position, count);

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

        protected NetworkWriter(byte[] data, int position) : base(data, position) { }

        public new static class Pool
        {
            static ConcurrentQueue<NetworkWriter> Queue;

            public static int Count => Queue.Count;

            public const int DefaultBinaryAllocationSize = 1024;

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

                var binary = new byte[DefaultBinaryAllocationSize];
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