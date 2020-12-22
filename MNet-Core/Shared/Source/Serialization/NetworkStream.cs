using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    public abstract class NetworkStream
    {
        protected byte[] data;
        public byte[] Data { get { return data; } }

        public int Capacity => data.Length;

        public int Size => Position;

        int _position;
        public int Position
        {
            get => _position;
            set
            {
                if (value < 0 || value > Capacity)
                    throw new IndexOutOfRangeException();

                _position = value;

                Remaining = Capacity - _position;
            }
        }

        public int Remaining { get; protected set; }

        public const uint DefaultResizeLength = 512;

        protected void Resize(uint extra)
        {
            var value = new byte[Capacity + extra];

            Buffer.BlockCopy(data, 0, value, 0, Position);

            this.data = value;
        }
        protected void ResizeToFit(int capacity)
        {
            if (capacity <= 0) throw new Exception($"Cannot Resize Network Buffer to Fit {capacity}");

            uint extra = DefaultResizeLength;

            while (capacity > Remaining + extra)
                extra += DefaultResizeLength;

            Resize(extra);
        }

        public void Shift(int start) => Shift(start, Position);
        public void Shift(int start, int end)
        {
            for (int i = start; i < end; i++) data[i - start] = data[i];

            Position = end - start;
        }

        public void Clear()
        {
            Position = 0;
        }

        public NetworkStream(byte[] data)
        {
            this.data = data;
            Position = 0;
        }

        //Static Utility

        public static void Clear(NetworkStream stream) => stream.Clear();

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

    public class NetworkWriter : NetworkStream
    {
        public byte[] ToArray() => ToArray(Position);
        public byte[] ToArray(int end) => ToArray(0, end);
        public byte[] ToArray(int start, int end)
        {
            var result = new byte[end - start];

            Buffer.BlockCopy(data, start, result, 0, result.Length);

            return result;
        }

        public void Insert(byte[] source)
        {
            var count = source.Length;

            if (count > Remaining) ResizeToFit(count);

            Buffer.BlockCopy(source, 0, data, Position, count);

            Position += count;
        }

        public void Replace(byte[] source, int position)
        {
            Buffer.BlockCopy(source, 0, data, position, source.Length);
        }

        public void Insert(byte value)
        {
            if (Remaining == 0) Resize(DefaultResizeLength);

            data[Position] = value;

            Position += 1;
        }

        #region Write
        public void Write<T>(T value)
        {
            if (ResolveNull(value)) return;

            if (ResolveExplicit(value)) return;

            if (ResolveImplicit(value)) return;

            throw FormatResolverException<T>();
        }

        public void Write(object value)
        {
            var type = value == null ? null : value.GetType();

            Write(value, type);
        }
        public void Write(object value, Type type)
        {
            if (ResolveNull(value, type)) return;

            if (ResolveAny(value, type)) return;

            throw FormatResolverException(type);
        }
        #endregion

        #region Resolve
        bool ResolveNull<T>(T value)
        {
            if (value == null)
            {
                Write(true); //Is Null Flag Value
                return true;
            }

            if (NetworkSerializationHelper.Nullable.Generic<T>.Is) Write(false); //Is Not Null Flag

            return false;
        }
        bool ResolveNull(object value, Type type)
        {
            if (value == null)
            {
                Write(true); //Is Null Flag Value
                return true;
            }

            if (NetworkSerializationHelper.Nullable.Any.Check(type)) Write(false); //Is Not Null Flag

            return false;
        }

        bool ResolveExplicit<T>(T value)
        {
            var resolver = NetworkSerializationExplicitResolver<T>.Instance;

            if (resolver == null) return false;

            resolver.SerializeExplicit(this, value);
            return true;
        }

        bool ResolveImplicit(object value)
        {
            var type = value.GetType();

            var resolver = NetworkSerializationResolver.Retrive(type);

            if (resolver == null) return false;

            resolver.SerializeImplicit(this, value, type);
            return true;
        }

        bool ResolveAny(object value, Type type)
        {
            var resolver = NetworkSerializationResolver.Retrive(type);

            if (resolver == null) return false;

            resolver.SerializeImplicit(this, value, type);
            return true;
        }
        #endregion

        public NetworkWriter(int size) : base(new byte[size]) { }

        //Static Utility
        public static ObjectPooler<NetworkWriter> Pool { get; protected set; }

        public static NetworkWriter Create() => new NetworkWriter(NetworkSerializer.DefaultBufferSize);

        static NetworkWriter()
        {
            Pool = new ObjectPooler<NetworkWriter>(Create, Clear);
        }
    }

    public class NetworkReader : NetworkStream
    {
        public virtual byte[] BlockCopy(int length)
        {
            var destination = new byte[length];

            Buffer.BlockCopy(data, Position, destination, 0, length);

            Position += length;

            return destination;
        }

        public void Set(byte[] data)
        {
            this.data = data;

            Clear();
        }

        #region Read
        public void Read<T>(out T value) => value = Read<T>();
        public T Read<T>()
        {
            if (ResolveNull<T>()) return default;

            T value = default;

            if (ResolveExplicit(ref value)) return value;

            if (ResolveImplicit(ref value)) return value;

            throw FormatResolverException<T>();
        }
        
        public object Read(Type type)
        {
            if (ResolveNull(type)) return null;

            if (ResolveAny(type, out var value)) return value;

            throw FormatResolverException(type);
        }
        #endregion

        #region Resolve
        bool ResolveNull<T>()
        {
            if (NetworkSerializationHelper.Nullable.Generic<T>.Is == false) return false;

            Read(out bool isNull);

            return isNull;
        }
        bool ResolveNull(Type type)
        {
            if (NetworkSerializationHelper.Nullable.Any.Check(type) == false) return false;

            Read(out bool isNull);

            return isNull;
        }

        bool ResolveExplicit<T>(ref T value)
        {
            var resolver = NetworkSerializationExplicitResolver<T>.Instance;

            if (resolver == null) return false;

            value = resolver.DeserializeExplicit(this);
            return true;
        }

        bool ResolveImplicit<T>(ref T value)
        {
            var type = typeof(T);

            object instance = null;

            if (ResolveImplicit(type, ref instance) == false) return false;

            try
            {
                value = (T)instance;
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"NetworkReader Trying to read {instance.GetType()} as {typeof(T)}");
            }
            catch (Exception)
            {
                throw;
            }

            return true;
        }
        bool ResolveImplicit(Type type, ref object value)
        {
            var resolver = NetworkSerializationResolver.Retrive(type);

            if (resolver == null) return false;

            value = resolver.DeserializeImplicit(this, type);
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

            value = resolver.DeserializeImplicit(this, type);
            return true;
        }
        #endregion

        public NetworkReader() : this(new byte[0]) { }
        public NetworkReader(byte[] data) : base(data) { }
    }
}