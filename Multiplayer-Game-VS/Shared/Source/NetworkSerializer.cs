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
        public const int BufferSize = 2048;

        public static byte[] Serialize(object instance)
        {
            var writer = new NetworkWriter(BufferSize);

            writer.Write(instance);

            var result = writer.ToArray();

            return result;
        }

        public static T Deserialize<T>(byte[] data)
            where T : new()
        {
            var reader = new NetworkReader(data);

            var result = reader.Read<T>();

            return result;
        }
        public static object Deserialize(byte[] data, Type type)
        {
            var reader = new NetworkReader(data);

            var result = reader.Read(type);

            return result;
        }
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

        public virtual void Dispose()
        {
            data = null;
        }

        public NetworkStream(byte[] data)
        {
            this.data = data;
        }

        public static bool IsNullable(Type type) => type.IsValueType == false;
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

            Buffer.BlockCopy(source, 0, data, Position, count);

            Position += count;
        }
        public void Insert(byte value)
        {
            data[Position] = value;

            Position += 1;
        }

        public void Write(object value)
        {
            if(value == null)
            {
                Write(true); //Is Null Flag Value
                return;
            }

            var type = value.GetType();

            if (IsNullable(type)) Write(false); //Is Not Null Flag

            var resolver = NetworkSerializationResolver.Collection.Retrive(type);
            if (resolver != null)
            {
                resolver.Serialize(this, value);
                return;
            }

            throw new NotImplementedException($"Type {type.Name} isn't supported for Network Serialization");
        }

        public NetworkWriter(int size) : base(new byte[size]) { }
    }

    public class NetworkReader : NetworkStream
    {
        public void Read<T>(out T value) => value = Read<T>();
        public T Read<T>()
        {
            var value = Read(typeof(T));

            try
            {
                return (T)value;
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"Trying to read {value.GetType()} as {typeof(T)}");
            }
            catch(Exception)
            {
                throw;
            }
        }
        public object Read(Type type)
        {
            if(IsNullable(type))
            {
                Read(out bool isNull);

                if (isNull) return null;
            }

            var resolver = NetworkSerializationResolver.Collection.Retrive(type);
            if (resolver != null) return resolver.Deserialize(this, type);

            throw new NotImplementedException($"Type {type.Name} isn't supported for Network Serialization");
        }

        public NetworkReader(byte[] data) : base(data) { }
    }

    public interface INetworkSerializable
    {
        void Serialize(NetworkWriter writer);

        void Deserialize(NetworkReader reader);
    }
}