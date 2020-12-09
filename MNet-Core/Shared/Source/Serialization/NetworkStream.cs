using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    public abstract class NetworkStream : IDisposable
    {
        protected byte[] data;
        public byte[] Data { get { return data; } }

        public int Capacity => data.Length;

        public int Size => Position;

        int position;
        public int Position
        {
            get => position;
            set
            {
                if (value < 0 || value > Capacity)
                    throw new IndexOutOfRangeException();

                position = value;
            }
        }

        public int Remaining => Capacity - position;

        public const uint DefaultResizeLength = 512;

        public virtual void Dispose()
        {
            data = null;
        }

        protected void Resize(uint extra)
        {
            var value = new byte[Capacity + extra];

            Buffer.BlockCopy(data, 0, value, 0, position);

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
            position = 0;
        }

        public NetworkStream(byte[] data)
        {
            this.data = data;
        }

        public static bool IsNullable(Type type) => NetworkSerializationHelper.Nullable.Check(type);
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
            var type = typeof(T);

            if (ResolveNull(value, type)) return;

            if (ResolveExplicit(value, type)) return;

            if (ResolveImplicit(value, type)) return;

            throw FormatResolverException(type);
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
        bool ResolveNull(object value, Type type)
        {
            if (value == null)
            {
                Write(true); //Is Null Flag Value
                return true;
            }

            if (IsNullable(type)) Write(false); //Is Not Null Flag

            return false;
        }

        bool ResolveExplicit<T>(T value, Type type)
        {
            var resolver = NetworkSerializationExplicitResolver<T>.Instance;

            if (resolver == null) return false;

            resolver.Serialize(this, value);
            return true;
        }

        bool ResolveImplicit(object value, Type type)
        {
            var resolver = NetworkSerializationImplicitResolver.Retrive(type);

            if (resolver == null) return false;

            resolver.Serialize(this, value, type);
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

        public NetworkWriter(int size) : base(new byte[size]) { }

        public static NotImplementedException FormatResolverException(Type type)
        {
            return new NotImplementedException($"Type {type} isn't supported for Network Serialization");
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

        #region Read
        public void Read<T>(out T value) => value = Read<T>();
        public T Read<T>()
        {
            var type = typeof(T);

            T value = default;

            if (ResolveNull(type, ref value)) return value;

            if (ResolveExplicit(type, ref value)) return value;

            if (ResolveImplicit(type, ref value)) return value;

            throw new NotImplementedException($"Type {type.Name} isn't supported for Network Serialization");
        }
        
        public object Read(Type type)
        {
            object value = default;

            if (ResolveNull(type, ref value)) return value;

            if (ResolveAny(type, ref value)) return value;

            throw new NotImplementedException($"Type {type.Name} isn't supported for Network Serialization");
        }
        #endregion

        #region Resolve
        bool ResolveNull<T>(Type type, ref T value)
        {
            if (IsNullable(type))
            {
                Read(out bool isNull);

                if (isNull)
                {
                    value = default;
                    return true;
                }
            }

            return false;
        }

        bool ResolveExplicit<T>(Type type, ref T value)
        {
            var resolver = NetworkSerializationExplicitResolver<T>.Instance;

            if (resolver == null) return false;

            value = resolver.Deserialize(this);
            return true;
        }

        bool ResolveImplicit<T>(Type type, ref T value)
        {
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
            var resolver = NetworkSerializationImplicitResolver.Retrive(type);

            if (resolver == null) return false;

            value = resolver.Deserialize(this, type);
            return true;
        }

        bool ResolveAny(Type type, ref object value)
        {
            var resolver = NetworkSerializationResolver.Retrive(type);

            if (resolver == null) return false;

            value = resolver.Deserialize(this, type);
            return true;
        }
        #endregion

        public NetworkReader(byte[] data) : base(data) { }
    }
}