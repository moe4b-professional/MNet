using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace Game.Fixed
{
    public static class NetworkSerializer
    {
        public const int BufferSize = 20480;

        public static byte[] Serialize<T>(T instance)
            where T : INetSerializable
        {
            var writer = new NetworkWriter(BufferSize);

            writer.WriteSerializable(instance);

            var result = writer.Read();

            return result;
        }
        public static byte[] Serialize(object instance)
        {
            var writer = new NetworkWriter(BufferSize);

            var result = writer.Read();

            return result;
        }

        public static T Deserialize<T>(byte[] data)
            where T : INetSerializable, new()
        {
            var reader = new NetworkReader(data);

            var result = reader.ReadSerializable<T>();

            return result;
        }
        public static object Deserialize(byte[] data, Type type)
        {
            var reader = new NetworkReader(data);

            var result = reader.ReadSerializable(type);

            return result;
        }
    }

    public partial class SampleObject : INetSerializable
    {
        public int number;

        public string text;

        public string[] array;

        public KeyValuePair<string, string> keyvalue;

        public Dictionary<string, string> dictionary;

        public void Deserialize(NetworkReader reader)
        {
            number = reader.ReadInt();
            text = reader.ReadString();
            array = reader.ReadArray<string>();
            keyvalue = reader.ReadKeyValuePair<string, string>();
            dictionary = reader.ReadDictionary<string, string>();
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteInt(number);
            writer.WriteString(text);
            writer.WriteArray(array);
            writer.WriteKeyValuePair(keyvalue);
            writer.WriteDictionary(dictionary);
        }
    }

    public abstract class NetworkDataOperator : IDisposable
    {
        protected byte[] data;
        public byte[] Data { get { return data; } }

        public int Size => data.Length;

        protected int position;
        public int Position { get { return position; } }

        public int Remaining => Size - position;

        public virtual void Dispose()
        {
            data = null;
        }

        public NetworkDataOperator(byte[] data)
        {
            this.data = data;
        }
    }

    public class NetworkWriter : NetworkDataOperator
    {
        public byte[] Read()
        {
            var result = new byte[position];

            Buffer.BlockCopy(data, 0, result, 0, position);

            return result;
        }

        public void Put(IList<byte> value)
        {
            for (int i = 0; i < value.Count; i++)
                Put(value[i]);
        }
        public void Put(byte value)
        {
            data[position] = value;

            position += 1;
        }

        #region Primitives
        public void WriteByte(byte value)
        {
            Put(value);
        }

        public void WriteInt(Int32 value)
        {
            var binary = BitConverter.GetBytes(value);

            Put(binary);
        }

        public void WriteString(string value)
        {
            var binary = Encoding.UTF8.GetBytes(value);

            WriteInt(binary.Length);

            Put(binary);
        }
        #endregion

        public void WriteSerializable<T>(T value)
            where T : INetSerializable
        {
            value.Serialize(this);
        }

        #region Collections
        public void WriteArray<T>(T[] array) => WriteList(array);
        public void WriteList<T>(IList<T> list)
        {
            WriteInt(list.Count);

            for (int i = 0; i < list.Count; i++)
                WriteBasic(list[i]);
        }
        
        public void WriteDictionary<Tkey, TValue>(Dictionary<Tkey, TValue> value)
        {
            WriteInt(value.Count);

            foreach (var pair in value)
                WriteKeyValue(pair.Key, pair.Value);
        }

        public void WriteKeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> pair) => WriteKeyValue(pair.Key, pair.Value);
        public void WriteKeyValue<TKey, TValue>(TKey key, TValue value)
        {
            WriteBasic(key);

            WriteBasic(value);
        }
        #endregion

        public void WriteBasic(object value)
        {
            if (value is byte)
            {
                WriteByte((byte)value);
                return;
            }

            if (value is Int32)
            {
                WriteInt((Int32)value);
                return;
            }

            if (value is string)
            {
                WriteString((string)value);
                return;
            }

            if (value is INetSerializable)
            {
                WriteSerializable(value as INetSerializable);
                return;
            }

            else throw new NotImplementedException();
        }

        public NetworkWriter(int size) : base(new byte[size]) { }
    }

    public class NetworkReader : NetworkDataOperator
    {
        #region Primitives
        public byte ReadByte()
        {
            var result = data[position];

            position += 1;

            return result;
        }

        public Int32 ReadInt()
        {
            var result = BitConverter.ToInt32(data, position);

            position += sizeof(Int32);

            return result;
        }

        public string ReadString()
        {
            var size = ReadInt();

            if (size == 0)
                return string.Empty;

            var result = Encoding.UTF8.GetString(data, position, size);

            position += size;

            return result;
        }
        #endregion

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
        public T[] ReadArray<T>()
        {
            var length = ReadInt();

            var array = new T[length];

            for (int i = 0; i < length; i++)
                array[i] = ReadBasic<T>();

            return array;
        }
        public List<T> ReadList<T>()
        {
            var length = ReadInt();

            var list = new List<T>(length);

            for (int i = 0; i < length; i++)
            {
                var item = ReadBasic<T>();

                list.Add(item);
            }

            return list;
        }

        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>()
        {
            var count = ReadInt();

            var dictionary = new Dictionary<TKey, TValue>(count);

            for (int i = 0; i < count; i++)
            {
                var pair = ReadKeyValuePair<TKey, TValue>();

                dictionary.Add(pair.Key, pair.Value);
            }

            return dictionary;
        }

        public KeyValuePair<TKey, TValue> ReadKeyValuePair<TKey, TValue>()
        {
            var key = ReadBasic<TKey>();

            var value = ReadBasic<TValue>();

            return new KeyValuePair<TKey, TValue>(key, value);
        }

        #endregion

        public T ReadBasic<T>() => (T)ReadBasic(typeof(T));
        public object ReadBasic(Type type)
        {
            if (type == typeof(byte)) return ReadByte();

            if (type == typeof(Int32)) return ReadInt();

            if (type == typeof(string)) return ReadString();

            if (typeof(INetSerializable).IsAssignableFrom(type)) return ReadSerializable(type);

            throw new NotImplementedException();
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
        public Type Type { get; protected set; }

        public abstract void Serialize(NetworkWriter writer, object type);

        public abstract object Deserialize(NetworkReader reader);

        public NetworkSerializationResolver(Type type)
        {
            this.Type = type;
        }

        public static class Collection
        {
            public static List<NetworkSerializationResolver> List { get; private set; }

            static Collection()
            {
                List = new List<NetworkSerializationResolver>();

                var target = typeof(NetworkSerializationResolver);

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (target.IsAssignableFrom(type) == false) continue;

                        var constructor = type.GetConstructor(Type.EmptyTypes);

                        if (constructor == null)
                            throw new InvalidOperationException($"{type.FullName} needs to have an empty constructor to be registered as a {nameof(NetworkSerializationResolver)}");

                        var instance = Activator.CreateInstance(type);
                    }
                }
            }
        }
    }

    public class DateTimeNetworkSerializationResolver : NetworkSerializationResolver
    {
        public override void Serialize(NetworkWriter writer, object type)
        {
            var date = (DateTime)type;

            writer.WriteString(date.ToLongDateString());
        }

        public override object Deserialize(NetworkReader reader)
        {
            var text = reader.ReadString();

            if (DateTime.TryParse(text, out var date))
                return date;
            else
                return new DateTime();
        }

        public DateTimeNetworkSerializationResolver() : base(typeof(DateTime)) { }
    }
}