using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Collections;
using System.Reflection;
using System.ComponentModel;
using System.Net;

namespace MNet
{
    [Preserve]
    public abstract class NetworkSerializationResolver
    {
        public abstract bool CanResolve(Type type);

        public abstract void Serialize(NetworkWriter writer, object instance);

        public abstract object Deserialize(NetworkReader reader, Type type);

        public NetworkSerializationResolver() { }

        //Static Utility
        public static List<NetworkSerializationResolver> Explicit { get; private set; }
        public static List<NetworkSerializationResolver> Implicit { get; private set; }

        public static Dictionary<Type, NetworkSerializationResolver> Dictionary { get; private set; }

        static readonly object SyncLock = new object();

        public static NetworkSerializationResolver Retrive(Type type)
        {
            lock (SyncLock)
            {
                if (Dictionary.TryGetValue(type, out var value)) return value;

                for (int i = 0; i < Explicit.Count; i++)
                {
                    if (Explicit[i].CanResolve(type))
                    {
                        Dictionary.Add(type, Explicit[i]);

                        return Explicit[i];
                    }
                }

                for (int i = 0; i < Implicit.Count; i++)
                {
                    if (Implicit[i].CanResolve(type))
                    {
                        Dictionary.Add(type, Implicit[i]);

                        return Implicit[i];
                    }
                }

                return null;
            }
        }

        static NetworkSerializationResolver()
        {
            Explicit = new List<NetworkSerializationResolver>();
            Implicit = new List<NetworkSerializationResolver>();

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

                    if (instance is NetworkSerializationImplicitResolver)
                        Implicit.Add(instance);
                    else
                        Explicit.Add(instance);
                }
            }
        }
    }

    #region Explicit
    [Preserve]
    public abstract class NetworkSerializationExplicitResolver<T> : NetworkSerializationResolver
    {
        public static NetworkSerializationExplicitResolver<T> Instance { get; private set; }

        public Type Target { get; private set; } = typeof(T);

        public override bool CanResolve(Type type) => type == Target;

        public override void Serialize(NetworkWriter writer, object instance) => Serialize(writer, (T)instance);
        public abstract void Serialize(NetworkWriter writer, T value);

        public override object Deserialize(NetworkReader reader, Type type) => Deserialize(reader);
        public abstract T Deserialize(NetworkReader reader);

        public NetworkSerializationExplicitResolver()
        {
            Instance = this;
        }
    }

    #region Primitive
    [Preserve]
    public sealed class ByteNetworkSerializationResolver : NetworkSerializationExplicitResolver<byte>
    {
        public override void Serialize(NetworkWriter writer, byte value)
        {
            writer.Insert(value);
        }

        public override byte Deserialize(NetworkReader reader)
        {
            var value = reader.Data[reader.Position];

            reader.Position += 1;

            return value;
        }
    }

    [Preserve]
    public sealed class BoolNetworkSerializationResolver : NetworkSerializationExplicitResolver<bool>
    {
        public override void Serialize(NetworkWriter writer, bool value)
        {
            writer.Write(value ? (byte)1 : (byte)0);
        }

        public override bool Deserialize(NetworkReader reader)
        {
            reader.Read(out byte value);

            return value == 0 ? false : true;
        }
    }

    [Preserve]
    public sealed class ShortNetworkSerializationResolver : NetworkSerializationExplicitResolver<short>
    {
        public override void Serialize(NetworkWriter writer, short value)
        {
            var binary = BitConverter.GetBytes(value);

            writer.Insert(binary);
        }

        public override short Deserialize(NetworkReader reader)
        {
            var value = BitConverter.ToInt16(reader.Data, reader.Position);

            reader.Position += sizeof(short);

            return value;
        }
    }
    [Preserve]
    public sealed class UShortNetworkSerializationResolver : NetworkSerializationExplicitResolver<ushort>
    {
        public override void Serialize(NetworkWriter writer, ushort value)
        {
            var binary = BitConverter.GetBytes(value);

            writer.Insert(binary);
        }

        public override ushort Deserialize(NetworkReader reader)
        {
            var value = BitConverter.ToUInt16(reader.Data, reader.Position);

            reader.Position += sizeof(ushort);

            return value;
        }
    }

    [Preserve]
    public sealed class IntNetworkSerializationResolver : NetworkSerializationExplicitResolver<int>
    {
        public override void Serialize(NetworkWriter writer, int value)
        {
            var binary = BitConverter.GetBytes(value);

            writer.Insert(binary);
        }

        public override int Deserialize(NetworkReader reader)
        {
            var value = BitConverter.ToInt32(reader.Data, reader.Position);

            reader.Position += sizeof(int);

            return value;
        }
    }
    [Preserve]
    public sealed class UIntNetworkSerializationResolver : NetworkSerializationExplicitResolver<uint>
    {
        public override void Serialize(NetworkWriter writer, uint value)
        {
            var binary = BitConverter.GetBytes(value);

            writer.Insert(binary);
        }

        public override uint Deserialize(NetworkReader reader)
        {
            var value = BitConverter.ToUInt32(reader.Data, reader.Position);

            reader.Position += sizeof(uint);

            return value;
        }
    }

    [Preserve]
    public sealed class FloatNetworkSerializationResolver : NetworkSerializationExplicitResolver<float>
    {
        public override void Serialize(NetworkWriter writer, float value)
        {
            var binary = BitConverter.GetBytes(value);

            writer.Insert(binary);
        }

        public override float Deserialize(NetworkReader reader)
        {
            var value = BitConverter.ToSingle(reader.Data, reader.Position);

            reader.Position += sizeof(float);

            return value;
        }
    }

    [Preserve]
    public sealed class StringNetworkSerializationResolver : NetworkSerializationExplicitResolver<string>
    {
        public override void Serialize(NetworkWriter writer, string value)
        {
            if (value.Length == 0)
            {
                NetworkSerializationHelper.Length.Write(0, writer);
            }
            else
            {
                var binary = Encoding.UTF8.GetBytes(value);

                var count = binary.Length;

                NetworkSerializationHelper.Length.Write(count, writer);

                writer.Insert(binary);
            }
        }

        public override string Deserialize(NetworkReader reader)
        {
            NetworkSerializationHelper.Length.Read(out var count, reader);
            
            if (count == 0) return string.Empty;

            var value = Encoding.UTF8.GetString(reader.Data, reader.Position, count);

            reader.Position += count;

            return value;
        }
    }
    #endregion

    #region POCO
    [Preserve]
    public class GuidNetworkSerializationResolver : NetworkSerializationExplicitResolver<Guid>
    {
        public const byte Size = 16;

        public override void Serialize(NetworkWriter writer, Guid value)
        {
            var binary = value.ToByteArray();

            writer.Insert(binary);
        }

        public override Guid Deserialize(NetworkReader reader)
        {
            var binary = reader.BlockCopy(Size);

            var value = new Guid(binary);

            return value;
        }
    }

    [Preserve]
    public class DateTimeNetworkSerializationResolver : NetworkSerializationExplicitResolver<DateTime>
    {
        public override void Serialize(NetworkWriter writer, DateTime value)
        {
            var text = value.ToString();

            writer.Write(text);
        }

        public override DateTime Deserialize(NetworkReader reader)
        {
            reader.Read(out string text);

            if (DateTime.TryParse(text, out var date))
                return date;
            else
                return new DateTime();
        }
    }

    [Preserve]
    public class IPAddressNetworkSerializationResolver : NetworkSerializationExplicitResolver<IPAddress>
    {
        public override void Serialize(NetworkWriter writer, IPAddress value)
        {
            var bytes = value.GetAddressBytes();

            byte length = (byte)bytes.Length;

            writer.Write(length);
            writer.Insert(bytes);
        }

        public override IPAddress Deserialize(NetworkReader reader)
        {
            reader.Read(out byte length);

            var bytes = reader.BlockCopy(length);

            var value = new IPAddress(bytes);

            return value;
        }
    }
    #endregion
    [Preserve]
    public sealed class ByteArrayNetworkSerializationResolver : NetworkSerializationExplicitResolver<byte[]>
    {
        public override void Serialize(NetworkWriter writer, byte[] value)
        {
            writer.Write(value.Length);

            writer.Insert(value);
        }

        public override byte[] Deserialize(NetworkReader reader)
        {
            reader.Read(out int length);

            var value = reader.BlockCopy(length);

            return value;
        }
    }
    #endregion

    #region Implicit
    [Preserve]
    public abstract class NetworkSerializationImplicitResolver : NetworkSerializationResolver
    {

    }

    [Preserve]
    public sealed class TupleNetworkSerializationImplicitResolver : NetworkSerializationImplicitResolver
    {
        public static HashSet<Type> Types { get; private set; }

        public override bool CanResolve(Type type)
        {
            if (type.IsGenericType == false) return false;

            return Types.Contains(type.GetGenericTypeDefinition());
        }

        public override void Serialize(NetworkWriter writer, object instance)
        {
            throw GetException();
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            throw GetException();
        }

        NotImplementedException GetException() => new NotImplementedException("Tuple Serialization is Still not Supported, Please use NetTuple instead");

        static TupleNetworkSerializationImplicitResolver()
        {
            Types = new HashSet<Type>()
            {
                typeof(Tuple<>),
                typeof(Tuple<,>),
                typeof(Tuple<,,>),
                typeof(Tuple<,,,>),
                typeof(Tuple<,,,,>),
                typeof(Tuple<,,,,,>),
                typeof(Tuple<,,,,,,>),
                typeof(Tuple<,,,,,,,>),
            };
        }
    }

    [Preserve]
    public sealed class NetTupleNetworkSerializationImplicitResolver : NetworkSerializationImplicitResolver
    {
        public static Type Interface { get; private set; } = typeof(INetTuple);

        public override bool CanResolve(Type type) => Interface.IsAssignableFrom(type);

        public override void Serialize(NetworkWriter writer, object instance)
        {
            var value = instance as INetTuple;

            byte length = value.Length;
            writer.Write(length);

            for (int i = 0; i < value.Length; i++)
            {
                var code = NetworkPayload.GetCode(value[i]);
                writer.Write(code);
                writer.Write(value[i]);
            }
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            reader.Read(out byte length);

            var items = new object[length];

            for (int i = 0; i < length; i++)
            {
                reader.Read(out ushort code);

                var t = NetworkPayload.GetType(code);

                var element = reader.Read(t);

                items[i] = element;
            }

            var value = NetTuple.Create(type, items);

            return value;
        }
    }

    [Preserve]
    public sealed class INetworkSerializableResolver : NetworkSerializationImplicitResolver
    {
        public Type Interface => typeof(INetworkSerializable);

        public override bool CanResolve(Type type) => Interface.IsAssignableFrom(type);

        public override void Serialize(NetworkWriter writer, object instance)
        {
            var value = instance as INetworkSerializable;

            var context = new Context(writer);

            value.Select(context);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var value = Activator.CreateInstance(type) as INetworkSerializable;

            var context = new Context(reader);

            value.Select(context);

            return value;
        }

        public INetworkSerializableResolver() { }

        public class Context
        {
            public NetworkWriter Writer { get; protected set; }
            public bool IsWriting => Writer != null;

            public NetworkReader Reader { get; protected set; }
            public bool IsReading => Reader != null;

            public void Select<T>(ref T value)
            {
                if (IsWriting) Writer.Write(value);

                if (IsReading) Reader.Read(out value);
            }

            public Context(NetworkWriter writer)
            {
                this.Writer = writer;
            }
            public Context(NetworkReader reader)
            {
                this.Reader = reader;
            }
        }
    }

    #region Collection
    [Preserve]
    public sealed class ArrayNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public override bool CanResolve(Type type) => type.IsArray;

        public override void Serialize(NetworkWriter writer, object instance)
        {
            var array = (IList)instance;

            NetworkSerializationHelper.Length.Write(array.Count, writer);

            for (int i = 0; i < array.Count; i++)
                writer.Write(array[i]);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var element = type.GetElementType();

            NetworkSerializationHelper.Length.Read(out var length, reader);

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

    [Preserve]
    public sealed class ListNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public override bool CanResolve(Type type)
        {
            if (type.IsGenericType == false) return false;

            return type.GetGenericTypeDefinition() == typeof(List<>);
        }

        public override void Serialize(NetworkWriter writer, object instance)
        {
            var list = instance as IList;

            NetworkSerializationHelper.Length.Write(list.Count, writer);

            for (int i = 0; i < list.Count; i++)
                writer.Write(list[i]);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            NetworkSerializationHelper.Length.Read(out var count, reader);

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

    [Preserve]
    public sealed class DictionaryNetworkSerializationResolvery : NetworkSerializationImplicitResolver
    {
        public override bool CanResolve(Type type)
        {
            if (type.IsGenericType == false) return false;

            return type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        public override void Serialize(NetworkWriter writer, object instance)
        {
            var dictionary = (IDictionary)instance;

            NetworkSerializationHelper.Length.Write(dictionary.Count, writer);

            foreach (DictionaryEntry entry in dictionary)
            {
                writer.Write(entry.Key);
                writer.Write(entry.Value);
            }
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var arguments = type.GetGenericArguments();

            NetworkSerializationHelper.Length.Read(out var count, reader);

            var dictionary = Activator.CreateInstance(type, count) as IDictionary;

            for (int i = 0; i < count; i++)
            {
                var key = reader.Read(arguments[0]);
                var value = reader.Read(arguments[1]);

                dictionary.Add(key, value);
            }

            return dictionary;
        }

        public DictionaryNetworkSerializationResolvery() { }
    }
    #endregion

    [Preserve]
    public sealed class EnumNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public override bool CanResolve(Type type) => type.IsEnum;

        public override void Serialize(NetworkWriter writer, object instance)
        {
            var type = instance.GetType();

            var backing = Enum.GetUnderlyingType(type);

            var value = Convert.ChangeType(instance, backing);

            writer.Write(value);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var backing = Enum.GetUnderlyingType(type);

            var value = reader.Read(backing);

            var result = Enum.ToObject(type, value);

            return result;
        }
    }
    #endregion
}