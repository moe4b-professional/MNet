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
                if (Dictionary.TryGetValue(type, out var value))
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

    #region Primitive
    public class ByteNetworkSerializationResolver : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type) => type == typeof(byte);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = (byte)type;

            writer.Insert(value);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var value = reader.Data[reader.Position];

            reader.Position += 1;

            return value;
        }

        public ByteNetworkSerializationResolver() { }
    }

    public class ShortNetworkSerializationResolver : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type) => type == typeof(short);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = (short)type;

            var binary = BitConverter.GetBytes(value);

            writer.Insert(binary);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var value = BitConverter.ToInt16(reader.Data, reader.Position);

            reader.Position += sizeof(short);

            return value;
        }

        public ShortNetworkSerializationResolver() { }
    }

    public class UShortNetworkSerializationResolver : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type) => type == typeof(ushort);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = (ushort)type;

            var binary = BitConverter.GetBytes(value);

            writer.Insert(binary);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var value = BitConverter.ToUInt16(reader.Data, reader.Position);

            reader.Position += sizeof(ushort);

            return value;
        }

        public UShortNetworkSerializationResolver() { }
    }

    public class IntNetworkSerializationResolver : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type) => type == typeof(int);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = (int)type;

            var binary = BitConverter.GetBytes(value);

            writer.Insert(binary);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var value = BitConverter.ToInt32(reader.Data, reader.Position);

            reader.Position += sizeof(int);

            return value;
        }

        public IntNetworkSerializationResolver() { }
    }

    public class FloatNetworkSerializationResolver : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type) => type == typeof(float);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = (float)type;

            var binary = BitConverter.GetBytes(value);

            writer.Insert(binary);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var value = BitConverter.ToSingle(reader.Data, reader.Position);

            reader.Position += sizeof(float);

            return value;
        }

        public FloatNetworkSerializationResolver() { }
    }

    public class StringNetworkSerializationResolver : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type) => type == typeof(string);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = (string)type;

            if (value == null)
            {
                writer.Write(-1);
            }
            else if (value == string.Empty)
            {
                writer.Write(0);
            }
            else
            {
                var binary = Encoding.UTF8.GetBytes(value);

                var count = binary.Length;

                writer.Write(count);

                writer.Insert(binary);
            }
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            reader.Read(out int count);

            if (count < 0) return null;

            if (count == 0) return string.Empty;

            var value = Encoding.UTF8.GetString(reader.Data, reader.Position, count);

            reader.Position += count;

            return value;
        }

        public StringNetworkSerializationResolver() { }
    }
    #endregion

    #region POCO
    public class GuidNetworkSerializationResolver : NetworkSerializationResolver
    {
        public const byte Size = 16;

        public override bool CanResolve(Type type) => type == typeof(Guid);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = (Guid)type;

            var binary = value.ToByteArray();

            writer.Insert(binary);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var binary = new byte[Size];

            Buffer.BlockCopy(reader.Data, reader.Position, binary, 0, Size);

            var value = new Guid(binary);

            reader.Position += Size;

            return value;
        }

        public GuidNetworkSerializationResolver() { }
    }

    public class DateTimeNetworkSerializationResolver : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type) => type == typeof(DateTime);

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

    public class EnumNetworkSerializationResolver : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type) => type.IsEnum;

        public override void Serialize(NetworkWriter writer, object type)
        {
            short backing = Convert.ToInt16(type);

            writer.Write(backing);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            reader.Read(out short backing);

            var result = Enum.ToObject(type, backing);

            return result;
        }

        public EnumNetworkSerializationResolver() { }
    }

    public class INetSerializableNetworkSerializationResolver : NetworkSerializationResolver
    {
        public Type Interface => typeof(INetSerializable);

        public override bool CanResolve(Type type) => Interface.IsAssignableFrom(type);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = type as INetSerializable;

            value.Serialize(writer);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var value = Activator.CreateInstance(type) as INetSerializable;

            value.Deserialize(reader);

            return value;
        }

        public INetSerializableNetworkSerializationResolver() { }
    }

    #region Collection
    public class ArrayNetworkSerializationResolver : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type) => type.IsArray;

        public override void Serialize(NetworkWriter writer, object type)
        {
            var array = (Array)type;

            writer.Write(array.Length);

            for (int i = 0; i < array.Length; i++)
                writer.Write(array.GetValue(i));
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var element = type.GetElementType();

            reader.Read(out int length);

            var array = Array.CreateInstance(element, length);

            for (int i = 0; i < length; i++)
            {
                var value = reader.Read(element);

                array.SetValue(value, i);
            }

            return array;
        }

        public ArrayNetworkSerializationResolver() { }
    }

    public class ListNetworkSerializationResolver : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type)
        {
            if (type.IsGenericType == false) return false;

            return type.GetGenericTypeDefinition() == typeof(List<>);
        }

        public override void Serialize(NetworkWriter writer, object type)
        {
            var list = type as IList;

            writer.Write(list.Count);

            for (int i = 0; i < list.Count; i++)
                writer.Write(list[i]);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            reader.Read(out int count);

            var list = Activator.CreateInstance(type, count) as IList;

            var element = type.GetGenericArguments()[0];

            for (int i = 0; i < count; i++)
            {
                var value = reader.Read(element);

                list.Add(value);
            }

            return list;
        }

        public ListNetworkSerializationResolver() { }
    }

    public class DictionarNetworkSerializationResolvery : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type)
        {
            if (type.IsGenericType == false) return false;

            return type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        public override void Serialize(NetworkWriter writer, object type)
        {
            var dictionary = (IDictionary)type;

            if (dictionary == null) throw new Exception("Dictionary");

            var count = dictionary.Count;

            writer.Write(count);

            foreach (DictionaryEntry entry in dictionary)
            {
                writer.Write(entry.Key);
                writer.Write(entry.Value);
            }
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var arguments = type.GetGenericArguments();

            reader.Read(out int count);

            var dictionary = Activator.CreateInstance(type, count) as IDictionary;

            for (int i = 0; i < count; i++)
            {
                var key = reader.Read(arguments[0]);
                var value = reader.Read(arguments[1]);
            }

            return dictionary;
        }

        public DictionarNetworkSerializationResolvery() { }
    }
    #endregion
}