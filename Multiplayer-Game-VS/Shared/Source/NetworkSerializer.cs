using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Collections;

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

        #region Primitives
        public void Write(byte value) => WriteByte(value);
        public void WriteByte(byte value)
        {
            Insert(value);
        }

        public void Write(ushort value) => WriteUShort(value);
        public void WriteUShort(ushort value)
        {
            var binary = BitConverter.GetBytes(value);

            Insert(binary);
        }

        public void Write(short value) => WriteShort(value);
        public void WriteShort(short value)
        {
            var binary = BitConverter.GetBytes(value);

            Insert(binary);
        }

        public void Write(int value) => WriteInt(value);
        public void WriteInt(Int32 value)
        {
            var binary = BitConverter.GetBytes(value);

            Insert(binary);
        }

        public void Write(float value) => WriteFloat(value);
        public void WriteFloat(float value)
        {
            var binary = BitConverter.GetBytes(value);

            Insert(binary);
        }

        public void Write(string value) => WriteString(value);
        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteInt(-1);
            }
            else if (value == string.Empty)
            {
                WriteInt(0);
            }
            else
            {
                var binary = Encoding.UTF8.GetBytes(value);

                WriteInt(binary.Length);

                Insert(binary);
            }
        }
        #endregion

        public void WriteEnum<T>(T value)
            where T : struct, IComparable, IFormattable, IConvertible
        {
            WriteEnum(value);
        }
        public void WriteEnum(object value)
        {
            var backing = Convert.ToInt16(value);

            WriteShort(backing);
        }

        public void Write(Guid value) => WriteGuid(value);
        public void WriteGuid(Guid value)
        {
            var bytes = value.ToByteArray();

            Insert(bytes);
        }

        public void WriteSerializable<T>(T value)
            where T : INetSerializable
        {
            value.Serialize(this);
        }

        #region Collections
        public void Write<T>(T[] array) => WriteList(array);
        public void WriteArray<T>(T[] array) => WriteList(array);

        public void Write<T>(List<T> list) => WriteList(list);
        public void WriteList<T>(IList<T> list)
        {
            if(list == null)
            {
                WriteInt(-1);
            }
            else
            {
                WriteInt(list.Count);

                for (int i = 0; i < list.Count; i++)
                    Write(list[i]);
            }
        }

        public void Write<TKey, TValue>(Dictionary<TKey, TValue> dictionary) => WriteDictionary(dictionary);
        public void WriteDictionary<Tkey, TValue>(Dictionary<Tkey, TValue> value)
        {
            if(value == null)
            {
                WriteInt(-1);
            }
            else
            {
                Write(value.Count);

                foreach (var pair in value)
                    WriteKeyValue(pair.Key, pair.Value);
            }
        }

        public void Write<TKey, TValue>(KeyValuePair<TKey, TValue> pair) => WriteKeyValue(pair.Key, pair.Value);
        public void WriteKeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> pair) => WriteKeyValue(pair.Key, pair.Value);

        public void Write<TKey, TValue>(TKey key, TValue value) => WriteKeyValue(key, value);
        public void WriteKeyValue<TKey, TValue>(TKey key, TValue value)
        {
            Write(key);

            Write(value);
        }
        #endregion
        
        public void Write(object value)
        {
            var type = value.GetType();

            if (type.IsEnum) WriteEnum(value);

            if (typeof(INetSerializable).IsAssignableFrom(type))
            {
                WriteSerializable(value as INetSerializable);
                return;
            }

            if (TryWrite<byte>(value, type, WriteByte)) return;
            if (TryWrite<ushort>(value, type, WriteUShort)) return;
            if (TryWrite<short>(value, type, WriteShort)) return;
            if (TryWrite<float>(value, type, WriteFloat)) return;
            if (TryWrite<int>(value, type, WriteInt)) return;
            if (TryWrite<string>(value, type, WriteString)) return;

            var resolver = NetworkSerializationResolver.Collection.Retrive(type);
            if (resolver != null)
            {
                resolver.Serialize(this, value);
                return;
            }

            throw new NotImplementedException($"Type {type.Name} isn't supported for Network Serialization");
        }

        bool TryWrite<TType>(object value, Type type, Action<TType> action)
        {
            if (type == typeof(TType))
            {
                action((TType)value);

                return true;
            }

            return false;
        }

        public NetworkWriter(int size) : base(new byte[size]) { }
    }

    public class NetworkReader : NetworkStream
    {
        #region Primitives
        public void Read(out byte value) => value = ReadByte();
        public byte ReadByte()
        {
            var result = data[Position];

            Position += 1;

            return result;
        }

        public void Read(out ushort value) => value = ReadUShort();
        public ushort ReadUShort()
        {
            var result = BitConverter.ToUInt16(data, Position);

            Position += sizeof(ushort);

            return result;
        }

        public void Read(out short value) => value = ReadShort();
        public short ReadShort()
        {
            var result = BitConverter.ToInt16(data, Position);

            Position += sizeof(short);

            return result;
        }

        public void Read(out int value) => value = ReadInt();
        public int ReadInt()
        {
            var result = BitConverter.ToInt32(data, Position);

            Position += sizeof(int);

            return result;
        }

        public void Read(out float value) => value = ReadFloat();
        public float ReadFloat()
        {
            var result = BitConverter.ToSingle(data, Position);

            Position += sizeof(float);

            return result;
        }

        public void Read(out string value) => value = ReadString();
        public string ReadString()
        {
            var size = ReadInt();

            if (size < 0) return null;

            if (size == 0) return string.Empty;

            var result = Encoding.UTF8.GetString(data, Position, size);

            Position += size;

            return result;
        }
        #endregion

        public T ReadEnum<T>()
            where T : struct, IComparable, IFormattable, IConvertible
        {
            var result = (T)ReadEnum(typeof(T));

            return result;
        }
        public object ReadEnum(Type type)
        {
            var backing = ReadShort();

            var result = Enum.ToObject(type, backing);

            return result;
        }

        public T ReadSerializable<T>()
            where T : INetSerializable, new()
        {
            var result = new T();

            result.Deserialize(this);

            return result;
        }
        public INetSerializable ReadSerializable(Type type)
        {
            var result = Activator.CreateInstance(type) as INetSerializable;

            if (result == null) throw new ArgumentException($"{type.FullName} is not {nameof(INetSerializable)}");

            result.Deserialize(this);

            return result;
        }

        #region Collections
        public void Read<T>(out T[] value) => value = ReadArray<T>();
        public T[] ReadArray<T>()
        {
            Read(out int length);

            if (length < 0) return null;

            var array = new T[length];

            for (int i = 0; i < length; i++)
                array[i] = Read<T>();

            return array;
        }

        public void Read<T>(out List<T> value) => value = ReadList<T>();
        public List<T> ReadList<T>()
        {
            Read(out int length);

            if (length < 0) return null;

            var list = new List<T>(length);

            for (int i = 0; i < length; i++)
            {
                var item = Read<T>();

                list.Add(item);
            }

            return list;
        }

        public void Read<TKey, TValue>(out Dictionary<TKey, TValue> value) => value = ReadDictionary<TKey, TValue>();
        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>()
        {
            Read(out int count);

            if (count < 0) return null;

            var dictionary = new Dictionary<TKey, TValue>(count);

            for (int i = 0; i < count; i++)
            {
                var pair = ReadKeyValuePair<TKey, TValue>();

                dictionary.Add(pair.Key, pair.Value);
            }

            return dictionary;
        }

        public void Read<TKey, TValue>(out KeyValuePair<TKey, TValue> value) => value = ReadKeyValuePair<TKey, TValue>();
        public KeyValuePair<TKey, TValue> ReadKeyValuePair<TKey, TValue>()
        {
            var key = Read<TKey>();

            var value = Read<TValue>();

            return new KeyValuePair<TKey, TValue>(key, value);
        }
        #endregion

        public void Read<T>(out T value) => value = Read<T>();
        public T Read<T>() => (T)Read(typeof(T));
        public object Read(Type type)
        {
            if (type.IsEnum) return ReadEnum(type);

            if (typeof(INetSerializable).IsAssignableFrom(type)) return ReadSerializable(type);

            object result = null;

            if (TryRead(type, ref result, ReadByte)) return result;
            if (TryRead(type, ref result, ReadUShort)) return result;
            if (TryRead(type, ref result, ReadShort)) return result;
            if (TryRead(type, ref result, ReadInt)) return result;
            if (TryRead(type, ref result, ReadFloat)) return result;
            if (TryRead(type, ref result, ReadString)) return result;

            var resolver = NetworkSerializationResolver.Collection.Retrive(type);
            if (resolver != null) return resolver.Deserialize(this, type);

            throw new NotImplementedException($"Type {type.Name} isn't supported for Network Serialization");
        }

        bool TryRead<T>(Type type, ref object value, Func<T> method)
        {
            if (type == typeof(T))
            {
                value = method();

                return true;
            }

            return false;
        }

        public NetworkReader(byte[] data) : base(data) { }
    }

    public interface INetSerializable
    {
        void Serialize(NetworkWriter writer);

        void Deserialize(NetworkReader reader);
    }

    public abstract class NetworkSerializationResolver
    {
        public abstract bool CanResolve(Type type);

        public abstract void Serialize(NetworkWriter writer, object type);

        public abstract object Deserialize(NetworkReader reader, Type type);
        
        public static class Collection
        {
            public static List<NetworkSerializationResolver> List { get; private set; }

            public static Dictionary<Type, NetworkSerializationResolver> Dictionary { get; private set; }

            public static NetworkSerializationResolver Retrive(Type type)
            {
                if(Dictionary.TryGetValue(type, out var value))
                    return value;

                for (int i = 0; i < List.Count; i++)
                {
                    if (List[i].CanResolve(type))
                    {
                        Dictionary.Add(type, List[i]);

                        return List[i];
                    }
                }

                return null;
            }

            static Collection()
            {
                List = new List<NetworkSerializationResolver>();

                Dictionary = new Dictionary<Type, NetworkSerializationResolver>();

                var target = typeof(NetworkSerializationResolver);

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type == target) continue;

                        if (type.IsAbstract) continue;

                        if (target.IsAssignableFrom(type) == false) continue;

                        var constructor = type.GetConstructor(Type.EmptyTypes);

                        if (constructor == null)
                            throw new InvalidOperationException($"{type.FullName} needs to have an empty constructor to be registered as a {nameof(NetworkSerializationResolver)}");

                        var instance = Activator.CreateInstance(type) as NetworkSerializationResolver;

                        List.Add(instance);
                    }
                }
            }
        }
    }

    #region POCO Resolvers
    public abstract class PocoNetworkSerializationResolver : NetworkSerializationResolver
    {
        public abstract Type Target { get; }

        public override bool CanResolve(Type type) => type == Target;
    }

    public class ByteNetworkSerializationResolver : PocoNetworkSerializationResolver
    {
        public override Type Target => typeof(byte);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = (byte)type;

            writer.Write(value);
        }
        public override object Deserialize(NetworkReader reader, Type type)
        {
            reader.Read(out byte value);

            return value;
        }

        public ByteNetworkSerializationResolver() { }
    }

    public class ShortNetworkSerializationResolver : PocoNetworkSerializationResolver
    {
        public override Type Target => typeof(short);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = (short)type;

            writer.Write(value);
        }
        public override object Deserialize(NetworkReader reader, Type type)
        {
            reader.Read(out short value);

            return value;
        }

        public ShortNetworkSerializationResolver() { }
    }

    public class DateTimeNetworkSerializationResolver : PocoNetworkSerializationResolver
    {
        public override Type Target => typeof(DateTime);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = (DateTime)type;

            var text = value.ToString();

            writer.Write(text);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            reader.Read(out string text);

            if (DateTime.TryParse(text, out var date))
                return date;
            else
                return new DateTime();
        }

        public DateTimeNetworkSerializationResolver() { }
    }
    #endregion
}