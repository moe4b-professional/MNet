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

    public class PocoSerializer
    {
        public static void Test()
        {
            var data = new byte[1024];

            {
                var sample = new SampleObject()
                {
                    number = 42,
                    text = "Hello Serializer",
                    array = new string[]
                    {
                        "Welcome",
                        "To",
                        "Roayal",
                        "Mania"
                    },
                    keyvalue = new KeyValuePair<string, string>("One Ring", "Destruction"),
                    dictionary = new Dictionary<string, string>()
                    {
                        { "Name", "Moe4B" },
                        { "Level", "14" },
                    }
                };

                var writer = new NetworkWriter(data);

                writer.WriteSerializable(sample);

                Log.Info(writer.Position);
            }

            {
                var reader = new NetworkReader(data);

                var info = reader.ReadSerializable<SampleObject>();

                Log.Info(info.number);
                Log.Info(info.text);
                foreach (var item in info.array) Log.Info(item);
                Log.Info(info.keyvalue);
                foreach (var pair in info.dictionary) Log.Info(pair);
            }
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
            keyvalue = reader.ReadKeyValue<string, string>();
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

    public class NetworkWriter
    {
        protected byte[] data;
        public byte[] Data { get { return data; } }

        public int Size => data.Length;

        protected int position;
        public int Position { get { return position; } }

        public int Remaining => Size - position;

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

        public void WriteSerializable<T>(T value)
            where T : INetSerializable
        {
            value.Serialize(this);
        }

        public void WriteArray<T>(T[] array)
        {
            WriteInt(array.Length);

            for (int i = 0; i < array.Length; i++)
                WritePrimitive(array[i]);
        }

        public void WriteKeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> pair) => WriteKeyValue(pair.Key, pair.Value);
        public void WriteKeyValue<TKey, TValue>(TKey key, TValue value)
        {
            WritePrimitive(key);

            WritePrimitive(value);
        }
        public void WriteDictionary<Tkey, TValue>(Dictionary<Tkey, TValue> value)
        {
            WriteInt(value.Count);

            foreach (var pair in value)
                WriteKeyValue(pair.Key, pair.Value);
        }

        public void WritePrimitive(object value)
        {
            if (value is byte) WriteByte((byte)value);

            else if (value is Int32) WriteInt((Int32)value);

            else if (value is string) WriteString((string)value);

            else if (value is INetSerializable) WriteSerializable(value as INetSerializable);

            else throw new NotImplementedException(value.GetType().Name);
        }

        public NetworkWriter(byte[] data)
        {
            this.data = data;
        }
        public NetworkWriter(int size) : this(new byte[size]) { }
    }

    public class NetworkReader
    {
        protected byte[] data;
        public byte[] Data { get { return data; } }

        public int Size => data.Length;

        protected int position;
        public int Position { get { return position; } }

        public int Remaining => Size - position;

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

            result.Deserialize(this);

            return result;
        }

        public T[] ReadArray<T>()
        {
            var length = ReadInt();

            var array = new T[length];

            for (int i = 0; i < length; i++)
                array[i] = ReadPrimitive<T>();

            return array;
        }

        public KeyValuePair<TKey, TValue> ReadKeyValue<TKey, TValue>()
        {
            ReadPrimitive(out TKey key);

            ReadPrimitive(out TValue value);

            return new KeyValuePair<TKey, TValue>(key, value);
        }
        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>()
        {
            var count = ReadInt();

            var dictionary = new Dictionary<TKey, TValue>(count);

            for (int i = 0; i < count; i++)
            {
                var pair = ReadKeyValue<TKey, TValue>();

                dictionary.Add(pair.Key, pair.Value);
            }

            return dictionary;
        }

        public void ReadPrimitive<T>(out T value) => value = ReadPrimitive<T>();
        public T ReadPrimitive<T>() => (T)ReadPrimitive(typeof(T));
        public object ReadPrimitive(Type type)
        {
            if (type == typeof(byte)) return ReadByte();

            if (type == typeof(Int32)) return ReadInt();

            if (type == typeof(string)) return ReadString();

            if (typeof(INetSerializable).IsAssignableFrom(type)) return ReadSerializable(type);

            throw new NotImplementedException();
        }

        public NetworkReader(byte[] data)
        {
            this.data = data;
        }
    }

    public interface INetSerializable
    {
        void Serialize(NetworkWriter writer);

        void Deserialize(NetworkReader reader);
    }
}