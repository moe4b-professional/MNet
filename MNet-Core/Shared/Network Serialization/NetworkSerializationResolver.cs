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
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

namespace MNet
{
    [Preserve]
    public abstract class NetworkSerializationResolver
    {
        public abstract bool CanResolve(Type target);

        public abstract void Serialize(NetworkWriter writer, object instance, Type type);

        public abstract object Deserialize(NetworkReader reader, Type type);

        public NetworkSerializationResolver() { }

        //Static Utility
        public static List<NetworkSerializationResolver> Explicit { get; private set; }
        public static List<NetworkSerializationResolver> Implicit { get; private set; }

        public static Dictionary<Type, NetworkSerializationResolver> Dictionary { get; private set; }

        protected static readonly object SyncLock = new object();

        /// <summary>
        /// Method used to be called from derived classes' static constructors to make sure the base class's constructor is invoked as well
        /// Has no functionality, it just works !
        /// </summary>
        protected static void Initialiaze() { }

        public static NetworkSerializationResolver Retrive(Type type)
        {
            lock (SyncLock)
            {
                if (Dictionary.TryGetValue(type, out var value)) return value;

                bool CanResolve(NetworkSerializationResolver resolver) => resolver.CanResolve(type);

                if (value == null) value = Explicit.FirstOrDefault(CanResolve);
                if (value == null) value = Implicit.FirstOrDefault(CanResolve);

                if (value != null) Dictionary.Add(type, value);

                return value;
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

        public static Type Type => typeof(T);

        public override bool CanResolve(Type target) => Type == target;

        public override void Serialize(NetworkWriter writer, object instance, Type type) => Serialize(writer, (T)instance);
        public abstract void Serialize(NetworkWriter writer, T value);

        public override object Deserialize(NetworkReader reader, Type type) => Deserialize(reader);
        public abstract T Deserialize(NetworkReader reader);

        public NetworkSerializationExplicitResolver()
        {
            Instance = this;
        }

        static NetworkSerializationExplicitResolver()
        {
            //Explicitly Called to make sure that the base class's static constructor is called
            Initialiaze();
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

    /// <summary>
    /// Used to Serialize an Array of multiple types of objects
    /// </summary>
    [Preserve]
    public sealed class ObjectArrayNetworkSerializationResolver : NetworkSerializationExplicitResolver<object[]>
    {
        public override void Serialize(NetworkWriter writer, object[] value)
        {
            writer.Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                ushort code = NetworkPayload.GetCode(value[i]);

                writer.Write(code);

                writer.Write(value[i]);
            }
        }

        public override object[] Deserialize(NetworkReader reader)
        {
            reader.Read(out int length);

            var value = new object[length];

            for (int i = 0; i < length; i++)
            {
                reader.Read(out ushort code);

                var type = NetworkPayload.GetType(code);

                value[i] = reader.Read(type);
            }

            return value;
        }
    }

    /// <summary>
    /// Used to Serialize a List of multiple types of objects
    /// </summary>
    [Preserve]
    public sealed class ObjectListNetworkSerializationResolver : NetworkSerializationExplicitResolver<List<object>>
    {
        public override void Serialize(NetworkWriter writer, List<object> value)
        {
            writer.Write(value.Count);

            for (int i = 0; i < value.Count; i++)
            {
                ushort code = NetworkPayload.GetCode(value[i]);

                writer.Write(code);

                writer.Write(value[i]);
            }
        }

        public override List<object> Deserialize(NetworkReader reader)
        {
            reader.Read(out int count);

            var value = new List<object>(count);

            for (int i = 0; i < count; i++)
            {
                reader.Read(out ushort code);

                var type = NetworkPayload.GetType(code);

                var instance = reader.Read(type);

                value.Add(instance);
            }

            return value;
        }
    }
    #endregion

    #region Implicit
    [Preserve]
    public abstract class NetworkSerializationImplicitResolver : NetworkSerializationResolver
    {
        new public static NetworkSerializationResolver Retrive(Type target)
        {
            lock (SyncLock)
            {
                if (Dictionary.TryGetValue(target, out var value)) return value;

                bool CanResolve(NetworkSerializationResolver resolver) => resolver.CanResolve(target);

                if (value == null) value = Implicit.FirstOrDefault(CanResolve);

                if (value != null) Dictionary.Add(target, value);

                return value;
            }
        }

        static NetworkSerializationImplicitResolver()
        {
            //Explicitly Called to make sure that the base class's static constructor is called
            Initialiaze();
        }
    }

    [Preserve]
    public sealed class TupleNetworkSerializationImplicitResolver : NetworkSerializationImplicitResolver
    {
        public static Type Interface => typeof(ITuple);

        public override bool CanResolve(Type target) => Interface.IsAssignableFrom(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            var value = instance as ITuple;

            for (int i = 0; i < value.Length; i++)
                writer.Write(value[i]);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var arguments = type.GetGenericArguments();

            var items = new object[arguments.Length];

            for (int i = 0; i < arguments.Length; i++)
                items[i] = reader.Read(arguments[i]);

            var value = Activator.CreateInstance(type, items);

            return value;
        }

        NotImplementedException GetException() => new NotImplementedException("Tuple Serialization is Still not Supported, Please use NetTuple instead");
    }

    [Preserve]
    public sealed class NetTupleNetworkSerializationImplicitResolver : NetworkSerializationImplicitResolver
    {
        public static Type Interface => typeof(INetTuple);

        public override bool CanResolve(Type target) => Interface.IsAssignableFrom(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            var value = instance as INetTuple;

            for (int i = 0; i < value.Length; i++)
                writer.Write(value[i]);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var arguments = type.GetGenericArguments();

            var items = new object[arguments.Length];

            for (int i = 0; i < arguments.Length; i++)
                items[i] = reader.Read(arguments[i]);

            var value = NetTuple.Create(type, items);

            return value;
        }
    }

    [Preserve]
    public sealed class NullableNetworkSerializationImplicitResolver : NetworkSerializationImplicitResolver
    {
        public static Type GenericTypeDefinition => typeof(Nullable<>);

        public override bool CanResolve(Type target)
        {
            if (target.IsGenericType == false) return false;

            return target.GetGenericTypeDefinition() == GenericTypeDefinition;
        }

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            writer.Write(instance);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);

            var value = reader.Read(underlying);

            return Activator.CreateInstance(type, value);
        }

        static NullableNetworkSerializationImplicitResolver()
        {
            
        }
    }

    [Preserve]
    public sealed class INetworkSerializableResolver : NetworkSerializationImplicitResolver
    {
        public Type Interface => typeof(INetworkSerializable);

        public override bool CanResolve(Type target) => Interface.IsAssignableFrom(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
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
        public override bool CanResolve(Type target) => target.IsArray;

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            var array = instance as IList;

            NetworkSerializationHelper.Length.Write(array.Count, writer);

            NetworkSerializationHelper.CollectionElement.Retrieve(type, out var elementType);

            for (int i = 0; i < array.Count; i++) writer.Write(array[i], elementType);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            NetworkSerializationHelper.Length.Read(out var length, reader);

            NetworkSerializationHelper.CollectionElement.Retrieve(type, out var elementType);

            var array = Array.CreateInstance(elementType, length);

            for (int i = 0; i < length; i++)
            {
                var element = reader.Read(elementType);

                array.SetValue(element, i);
            }

            return array;
        }

        public ArrayNetworkSerializationResolver() { }
    }

    [Preserve]
    public sealed class ListNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public override bool CanResolve(Type target)
        {
            if (target.IsGenericType == false) return false;

            return target.GetGenericTypeDefinition() == typeof(List<>);
        }

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            var list = instance as IList;

            NetworkSerializationHelper.Length.Write(list.Count, writer);

            NetworkSerializationHelper.CollectionElement.Retrieve(type, out var elementType);

            for (int i = 0; i < list.Count; i++) writer.Write(list[i], elementType);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            NetworkSerializationHelper.Length.Read(out var count, reader);

            var list = Activator.CreateInstance(type, count) as IList;

            NetworkSerializationHelper.CollectionElement.Retrieve(type, out var elementType);

            for (int i = 0; i < count; i++)
            {
                var element = reader.Read(elementType);

                list.Add(element);
            }

            return list;
        }

        public ListNetworkSerializationResolver() { }
    }

    [Preserve]
    public sealed class DictionaryNetworkSerializationResolvery : NetworkSerializationImplicitResolver
    {
        public override bool CanResolve(Type target)
        {
            if (target.IsGenericType == false) return false;

            return target.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            var dictionary = (IDictionary)instance;

            NetworkSerializationHelper.Length.Write(dictionary.Count, writer);

            NetworkSerializationHelper.CollectionElement.Retrieve(type, out var keyType, out var valueType);

            foreach (DictionaryEntry entry in dictionary)
            {
                writer.Write(entry.Key, keyType);
                writer.Write(entry.Value, valueType);
            }
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            NetworkSerializationHelper.Length.Read(out var count, reader);

            var dictionary = Activator.CreateInstance(type, count) as IDictionary;

            NetworkSerializationHelper.CollectionElement.Retrieve(type, out var keyType, out var valueType);

            for (int i = 0; i < count; i++)
            {
                var key = reader.Read(keyType);
                var value = reader.Read(valueType);

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
        public override bool CanResolve(Type target) => target.IsEnum;

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
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