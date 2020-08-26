using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Collections;
using System.Reflection;

namespace Game.Shared
{
    public static class NetworkSerializer
    {
        public const int DefaultBufferSize = 2048;

        #region Serialize
        public static byte[] Serialize<T>(T instance) => Serialize(instance, DefaultBufferSize);
        public static byte[] Serialize<T>(T instance, int bufferSize)
        {
            using (var writer = new NetworkWriter(bufferSize))
            {
                writer.Write(instance);

                var result = writer.ToArray();

                return result;
            }
        }

        public static byte[] Serialize(object instance) => Serialize(instance, DefaultBufferSize);
        public static byte[] Serialize(object instance, int bufferSize)
        {
            using (var writer = new NetworkWriter(bufferSize))
            {
                writer.Write(instance);

                var result = writer.ToArray();

                return result;
            }
        }
        #endregion

        #region Deserialize
        public static T Deserialize<T>(byte[] data)
            where T : new()
        {
            using (var reader = new NetworkReader(data))
            {
                reader.Read(out T result);

                return result;
            }
        }

        public static object Deserialize(byte[] data, Type type)
        {
            using (var reader = new NetworkReader(data))
            {
                var result = reader.Read(type);

                return result;
            }
        }
        #endregion
    }

    public abstract class NetworkStream : IDisposable
    {
        protected byte[] data;
        public byte[] Data { get { return data; } }

        public int Size => data.Length;

        private int position;
        public int Position
        {
            get => position;
            set
            {
                if (value < 0 || value > Size)
                    throw new IndexOutOfRangeException();

                position = value;
            }
        }

        public int Remaining => Size - position;

        public const uint DefaultResizeLength = 512;

        public virtual void Dispose()
        {
            data = null;
        }

        protected void Resize(uint extra)
        {
            var value = new byte[Size + extra];

            Buffer.BlockCopy(data, 0, value, 0, position);

            this.data = value;
        }
        protected void ResizeToFit(int capacity)
        {
            if (capacity <= 0) throw new Exception($"Cannot Resize Network Buffer to Fit {capacity}");

            uint extra = DefaultResizeLength;

            while (capacity > Size + extra)
                extra += DefaultResizeLength;

            Resize(extra);
        }

        public NetworkStream(byte[] data)
        {
            this.data = data;
        }

        public static bool IsNullable(Type type) => NetworkNullable.Check(type);
    }

    public class NetworkWriter : NetworkStream
    {
        public byte[] ToArray()
        {
            var result = new byte[Position];

            Buffer.BlockCopy(data, 0, result, 0, Position);

            return result;
        }

        public void Insert(byte[] source)
        {
            var count = source.Length;

            if (count > Remaining) ResizeToFit(count);

            Buffer.BlockCopy(source, 0, data, Position, count);

            Position += count;
        }
        public void Insert(byte value)
        {
            if (Remaining == 0) Resize(DefaultResizeLength);

            try
            {
                data[Position] = value;
            }
            catch (Exception)
            {
                Log.Info(Remaining);
                throw;
            }

            Position += 1;
        }

        public void Write<T>(T value)
        {
            var type = typeof(T);

            if (WriteExplicit(value, type))
            {

            }
            else if (WriteImplicit(value, type))
            {

            }
            else
            {
                throw new NotImplementedException($"Type {type} isn't supported for Network Serialization");
            }
        }
        bool WriteExplicit<T>(T value, Type type)
        {
            var resolver = NetworkSerializationExplicitResolver<T>.Instance;

            if (resolver == null) return false;

            if (value == null)
            {
                Write(true); //Is Null Flag Value
                return true;
            }
            if (IsNullable(type)) Write(false); //Is Not Null Flag

            resolver.Serialize(this, value);

            return true;
        }
        bool WriteImplicit(object value, Type type)
        {
            var resolver = NetworkSerializationImplicitResolver.Retrive(type);

            if (resolver == null) return false;

            if (value == null)
            {
                Write(true); //Is Null Flag Value
                return true;
            }
            if (IsNullable(type)) Write(false); //Is Not Null Flag

            resolver.Serialize(this, value);

            return true;
        }

        public void Write(object value)
        {
            if (value == null)
            {
                Write(true); //Is Null Flag Value
                return;
            }

            var type = value.GetType();

            if (IsNullable(type)) Write(false); //Is Not Null Flag

            var resolver = NetworkSerializationResolver.Retrive(type);

            if(resolver == null)
                throw new NotImplementedException($"Type {type} isn't supported for Network Serialization");

            resolver.Serialize(this, value);
        }

        public NetworkWriter(int size) : base(new byte[size]) { }
    }

    public class NetworkReader : NetworkStream
    {
        public void Read<T>(out T value) => value = Read<T>();
        public T Read<T>()
        {
            var type = typeof(T);

            T value;

            if(ReadExplicit(out value, type))
            {

            }
            else if(ReadImplicit(out var instance, type))
            {
                try
                {
                    value = (T)instance;
                }
                catch (InvalidCastException)
                {
                    throw new InvalidCastException($"Trying to read {instance.GetType()} as {typeof(T)}");
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                throw new NotImplementedException($"Type {type.Name} isn't supported for Network Serialization");
            }

            return value;
        }
        bool ReadExplicit<T>(out T value, Type type)
        {
            var resolver = NetworkSerializationExplicitResolver<T>.Instance;

            if(resolver == null)
            {
                value = default(T);
                return false;
            }

            if (IsNullable(type))
            {
                Read(out bool isNull);

                if (isNull)
                {
                    value = default(T);
                    return true;
                }
            }

            value = resolver.Deserialize(this);
            return true;
        }
        bool ReadImplicit(out object value, Type type)
        {
            var resolver = NetworkSerializationImplicitResolver.Retrive(type);

            if (resolver == null)
            {
                value = null;
                return false;
            }

            if (IsNullable(type))
            {
                Read(out bool isNull);

                if (isNull)
                {
                    value = null;
                    return true;
                }
            }

            value = resolver.Deserialize(this, type);
            return true;
        }

        public object Read(Type type)
        {
            var serializer = NetworkSerializationResolver.Retrive(type);

            if(serializer == null)
                throw new NotImplementedException($"Type {type.Name} isn't supported for Network Serialization");

            if (IsNullable(type))
            {
                Read(out bool isNull);

                if (isNull) return null;
            }

            var value = serializer.Deserialize(this, type);

            return value;
        }

        public NetworkReader(byte[] data) : base(data) { }
    }

    public interface INetworkSerializable
    {
        void Serialize(NetworkWriter writer);

        void Deserialize(NetworkReader reader);
    }

    public static class NetworkNullable
    {
        public static Dictionary<Type, bool> Dictionary { get; private set; }

        public static bool Check(Type type)
        {
            if (Dictionary.TryGetValue(type, out var result)) return result;

            result = type.IsValueType == false;

            Dictionary.Add(type, result);

            return result;
        }

        static NetworkNullable()
        {
            Dictionary = new Dictionary<Type, bool>();
        }
    }

    public static class NetworkSerializationLengthHelper
    {
        public static void Writer(int source, NetworkWriter writer)
        {
            if (source > ushort.MaxValue)
                throw new Exception($"Cannot Serialize {source} as a ushort Code, It's Value is Above the Maximum Value of {ushort.MaxValue}");

            var length = (ushort)source;

            writer.Write(length);
        }

        public static void Read(out ushort length, NetworkReader reader) => reader.Read(out length);
    }
}