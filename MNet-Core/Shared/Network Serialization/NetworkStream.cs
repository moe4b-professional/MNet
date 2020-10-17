﻿using System;
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

        public int Size => data.Length;

        int position;
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

            while (capacity > Remaining + extra)
                extra += DefaultResizeLength;

            Resize(extra);
        }

        public NetworkStream(byte[] data)
        {
            this.data = data;
        }

        public static bool IsNullable(Type type) => NetworkSerializationHelper.Nullable.Check(type);
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

            if (resolver == null)
                throw new NotImplementedException($"Type {type} isn't supported for Network Serialization");

            resolver.Serialize(this, value);
        }

        public NetworkWriter(int size) : base(new byte[size]) { }
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

        public void Read<T>(out T value) => value = Read<T>();
        public T Read<T>()
        {
            var type = typeof(T);

            if (ReadExplicit(out T value, type)) return value;

            if (ReadImplicit(out var instance, type))
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

                return value;
            }

            throw new NotImplementedException($"Type {type.Name} isn't supported for Network Serialization");
        }
        bool ReadExplicit<T>(out T value, Type type)
        {
            var resolver = NetworkSerializationExplicitResolver<T>.Instance;

            if (resolver == null)
            {
                value = default;
                return false;
            }

            if (IsNullable(type))
            {
                Read(out bool isNull);

                if (isNull)
                {
                    value = default;
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

            if (serializer == null)
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
}