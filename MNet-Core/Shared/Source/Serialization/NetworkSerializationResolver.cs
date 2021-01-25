using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.Net;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

using System.Linq.Expressions;

namespace MNet
{
    [Preserve]
    public abstract partial class NetworkSerializationResolver
    {
        public abstract bool CanResolve(Type target);

        public abstract void Serialize(NetworkWriter writer, object instance, Type type);

        protected static bool WriteNull(NetworkWriter writer, object value)
        {
            if (value == null)
            {
                writer.Insert(1); //Is Null Flag Value
                return true;
            }
            else
            {
                writer.Insert(0); //Is Not Null Flag
                return false;
            }
        }
        protected static bool WriteNull(NetworkWriter writer, object instance, Type type)
        {
            if (instance == null)
            {
                writer.Insert(1); //Is Null Flag Value
                return true;
            }

            if (NetworkSerializationHelper.Nullable.Any.Check(type)) writer.Insert(0); //Is Not Null Flag

            return false;
        }

        public abstract object Deserialize(NetworkReader reader, Type type);

        protected static bool ReadNull(NetworkReader reader)
        {
            return reader.Next() == 1 ? true : false;
        }
        protected static bool ReadNull(NetworkReader reader, Type type)
        {
            if (NetworkSerializationHelper.Nullable.Any.Check(type) == false) return false;

            return ReadNull(reader);
        }

        public NetworkSerializationResolver() { }

        //Static Utility
        public static List<NetworkSerializationResolver> Implicits { get; private set; }
        public static List<NetworkSerializationResolver> Explicits { get; private set; }

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

                //First, Look for Explicit Resolvers
                if (value == null) value = Explicits.FirstOrDefault(CanResolve);

                //If none, the look for a Dynamic Resolver (Generic Resolver)
                if (value == null) value = DynamicNetworkSerialization.Resolve(type);

                //If none, then finally look for an Implicit Resolver
                if (value == null) value = Implicits.FirstOrDefault(CanResolve);

                if (value != null) Dictionary.Add(type, value);

                return value;
            }
        }

        static NetworkSerializationResolver()
        {
            Implicits = new List<NetworkSerializationResolver>();
            Explicits = new List<NetworkSerializationResolver>();

            Dictionary = new Dictionary<Type, NetworkSerializationResolver>();

            var target = typeof(NetworkSerializationResolver);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type == target) continue;

                    if (type.IsAbstract) continue;

                    if (type.ContainsGenericParameters) continue;

                    if (target.IsAssignableFrom(type) == false) continue;

                    var constructor = type.GetConstructor(Type.EmptyTypes);

                    if (constructor == null)
                        throw new InvalidOperationException($"{type.FullName} needs to have an empty constructor to be registered as a {nameof(NetworkSerializationResolver)}");

                    var instance = Activator.CreateInstance(type) as NetworkSerializationResolver;

                    if (instance is NetworkSerializationImplicitResolver)
                        Implicits.Add(instance);
                    else
                        Explicits.Add(instance);
                }
            }
        }
    }

    public static class DynamicNetworkSerialization
    {
        public static List<ResolveDelegate> Resolvers { get; private set; }
        public delegate void ResolveDelegate(Type type, ref NetworkSerializationResolver resolver);

        public static bool Enabled { get; set; } = true;

        public static NetworkSerializationResolver Resolve(Type type)
        {
            if (Enabled == false) return null;

            NetworkSerializationResolver resolver = null;

            for (int i = 0; i < Resolvers.Count; i++)
            {
                Resolvers[i].Invoke(type, ref resolver);

                if (resolver == null) continue;

                return resolver;
            }

            return null;
        }

        public static void RegisterMethod(ResolveDelegate method)
        {
            Resolvers.Add(method);
        }

        #region Default Resolvers
        static void ResolveINetworkSerilizable(Type type, ref NetworkSerializationResolver resolver)
        {
            if (INetworkSerializableResolver.IsValid(type) == false) return;

            resolver = ConstructResolver(typeof(INetworkSerializableResolver<>), type);
        }
        static void ResolveIManualNetworkSerilizable(Type type, ref NetworkSerializationResolver resolver)
        {
            if (IManualNetworkSerializableResolver.IsValid(type) == false) return;

            resolver = ConstructResolver(typeof(IManualNetworkSerializableResolver<>), type);
        }

        static void ResolveEnum(Type type, ref NetworkSerializationResolver resolver)
        {
            if (EnumNetworkSerializationResolver.IsValid(type) == false) return;

            var backing = NetworkSerializationHelper.Enum.UnderlyingType.Retrieve(type);

            resolver = ConstructResolver(typeof(EnumNetworkSerializationResolver<,>), type, backing);
        }

        static void ResolveNullable(Type type, ref NetworkSerializationResolver resolver)
        {
            if (NullableNetworkSerializationResolver.IsValid(type) == false) return;

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(NullableNetworkSerializationResolver<>), argument);
        }

        static void ResolveTuple(Type type, ref NetworkSerializationResolver resolver)
        {
            if (type.IsGenericType == false) return;
            if (type.GetGenericTypeDefinition() != typeof(ValueTuple<>)) return;

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type[] arguments);

            switch (arguments.Length)
            {
                case 2:
                    ConstructResolver(typeof(ValueTuple<,>), arguments);
                    return;

                case 3:
                    ConstructResolver(typeof(ValueTuple<,,>), arguments);
                    return;

                case 4:
                    ConstructResolver(typeof(ValueTuple<,,,>), arguments);
                    return;

                case 5:
                    ConstructResolver(typeof(ValueTuple<,,,,>), arguments);
                    return;

                case 6:
                    ConstructResolver(typeof(ValueTuple<,,,,,>), arguments);
                    return;

                case 7:
                    ConstructResolver(typeof(ValueTuple<,,,,,,>), arguments);
                    return;

                case 8:
                    ConstructResolver(typeof(ValueTuple<,,,,,,>), arguments);
                    return;
            }
        }

        static void ResolveArray(Type type, ref NetworkSerializationResolver resolver)
        {
            if (ArrayNetworkSerializationResolver.IsValid(type) == false) return;

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(ArrayNetworkSerializationResolver<>), argument);
        }
        static void ResolveArraySegment(Type type, ref NetworkSerializationResolver resolver)
        {
            if (ArraySegmentNetworkSerializationResolver.IsValid(type) == false) return;

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(ArraySegmentNetworkSerializationResolver<>), argument);
        }
        static void ResolveList(Type type, ref NetworkSerializationResolver resolver)
        {
            if (ListNetworkSerializationResolver.IsValid(type) == false) return;

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(ListNetworkSerializationResolver<>), argument);
        }
        static void ResolveHashSet(Type type, ref NetworkSerializationResolver resolver)
        {
            if (HashSetNetworkSerializationResolver.IsValid(type) == false) return;

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(HashSetNetworkSerializationResolver<>), argument);
        }
        static void ResolveDictionary(Type type, ref NetworkSerializationResolver resolver)
        {
            if (DictionaryNetworkSerializationResolver.IsValid(type) == false) return;

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type key, out Type value);

            resolver = ConstructResolver(typeof(DictionaryNetworkSerializationResolver<,>), key, value);
        }
        #endregion

        public static NetworkSerializationResolver ConstructResolver(Type definition, params Type[] arguments)
        {
            var target = definition.MakeGenericType(arguments);

            var resolver = Activator.CreateInstance(target) as NetworkSerializationResolver;

            return resolver;
        }

        static DynamicNetworkSerialization()
        {
            Resolvers = new List<ResolveDelegate>();

            RegisterMethod(ResolveINetworkSerilizable);
            RegisterMethod(ResolveIManualNetworkSerilizable);

            RegisterMethod(ResolveEnum);

            RegisterMethod(ResolveNullable);

            RegisterMethod(ResolveTuple);

            RegisterMethod(ResolveArray);
            RegisterMethod(ResolveArraySegment);
            RegisterMethod(ResolveList);
            RegisterMethod(ResolveHashSet);
            RegisterMethod(ResolveDictionary);
        }
    }

    #region Explicit
    [Preserve]
    public abstract class NetworkSerializationExplicitResolver<T> : NetworkSerializationResolver
    {
        public static NetworkSerializationExplicitResolver<T> Instance;

        public static Type Type => typeof(T);

        public virtual bool CanResolveDerivatives => false;

        public override bool CanResolve(Type target)
        {
            if (CanResolveDerivatives)
                return Type.IsAssignableFrom(target);
            else
                return Type == target;
        }

        public abstract void Serialize(NetworkWriter writer, T instance);
        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            if (instance is T value)
                Serialize(writer, value);
            else if (instance == null)
                Serialize(writer, default);
            else
                throw new InvalidCastException($"Cannot Use {GetType()} to Serialize {type} Explicitly as {type} Cannot be cast to {typeof(T)}");
        }

        protected static bool WriteNull(NetworkWriter writer, T value)
        {
            if (value == null)
            {
                writer.Insert(1); //Is Null Flag Value
                return true;
            }
            else
            {
                writer.Insert(0); //Is Not Null Flag
                return false;
            }
        }

        public abstract T Deserialize(NetworkReader reader);
        public override object Deserialize(NetworkReader reader, Type type)
        {
            return Deserialize(reader);
        }

        public NetworkSerializationExplicitResolver()
        {
            Instance = this;
        }

        //Static

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
        public override void Serialize(NetworkWriter writer, byte instance)
        {
            writer.Insert(instance);
        }

        public override byte Deserialize(NetworkReader reader)
        {
            return reader.Next();
        }
    }

    [Preserve]
    public sealed class BoolNetworkSerializationResolver : NetworkSerializationExplicitResolver<bool>
    {
        public override void Serialize(NetworkWriter writer, bool instance)
        {
            writer.Write(instance ? (byte)1 : (byte)0);
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
        public override void Serialize(NetworkWriter writer, short instance)
        {
            var binary = BitConverter.GetBytes(instance);

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
        public override void Serialize(NetworkWriter writer, ushort instance)
        {
            var binary = BitConverter.GetBytes(instance);

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
        public override void Serialize(NetworkWriter writer, int instance)
        {
            var binary = BitConverter.GetBytes(instance);

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
        public override void Serialize(NetworkWriter writer, uint instance)
        {
            var binary = BitConverter.GetBytes(instance);

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
    public sealed class LongNetworkSerializationResolver : NetworkSerializationExplicitResolver<long>
    {
        public override void Serialize(NetworkWriter writer, long instance)
        {
            var binary = BitConverter.GetBytes(instance);

            writer.Insert(binary);
        }

        public override long Deserialize(NetworkReader reader)
        {
            var value = BitConverter.ToInt64(reader.Data, reader.Position);

            reader.Position += sizeof(long);

            return value;
        }
    }

    [Preserve]
    public sealed class ULongNetworkSerializationResolver : NetworkSerializationExplicitResolver<ulong>
    {
        public override void Serialize(NetworkWriter writer, ulong instance)
        {
            var binary = BitConverter.GetBytes(instance);

            writer.Insert(binary);
        }

        public override ulong Deserialize(NetworkReader reader)
        {
            var value = BitConverter.ToUInt64(reader.Data, reader.Position);

            reader.Position += sizeof(ulong);

            return value;
        }
    }

    [Preserve]
    public sealed class FloatNetworkSerializationResolver : NetworkSerializationExplicitResolver<float>
    {
        public override void Serialize(NetworkWriter writer, float instance)
        {
            var binary = BitConverter.GetBytes(instance);

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
    public sealed class DoubleNetworkSerializationResolver : NetworkSerializationExplicitResolver<double>
    {
        public override void Serialize(NetworkWriter writer, double instance)
        {
            var binary = BitConverter.GetBytes(instance);

            writer.Insert(binary);
        }

        public override double Deserialize(NetworkReader reader)
        {
            var value = BitConverter.ToDouble(reader.Data, reader.Position);

            reader.Position += sizeof(double);

            return value;
        }
    }

    [Preserve]
    public sealed class StringNetworkSerializationResolver : NetworkSerializationExplicitResolver<string>
    {
        public override void Serialize(NetworkWriter writer, string instance)
        {
            if (instance == null)
            {
                NetworkSerializationHelper.Length.Write(writer, 0);
            }
            else if (instance.Length == 0)
            {
                NetworkSerializationHelper.Length.Write(writer, 1);
            }
            else
            {
                var binary = Encoding.UTF8.GetBytes(instance);

                var length = binary.Length + 1;

                NetworkSerializationHelper.Length.Write(writer, length);

                writer.Insert(binary);
            }
        }

        public override string Deserialize(NetworkReader reader)
        {
            NetworkSerializationHelper.Length.Read(reader, out var length);

            if (length == 0) return null;
            if (length == 1) return string.Empty;

            length -= 1;

            var value = Encoding.UTF8.GetString(reader.Data, reader.Position, length);

            reader.Position += length;

            return value;
        }
    }
    #endregion

    [Preserve]
    public class TypeNetworkSerializationResolver : NetworkSerializationExplicitResolver<Type>
    {
        public override void Serialize(NetworkWriter writer, Type instance)
        {
            var code = NetworkPayload.GetCode(instance);

            writer.Write(code);
        }

        public override Type Deserialize(NetworkReader reader)
        {
            reader.Read(out byte code);

            var value = NetworkPayload.GetType(code);

            return value;
        }
    }

    [Preserve]
    public class NetworkMessageSerializationResolver : NetworkSerializationExplicitResolver<NetworkMessage>
    {
        public override void Serialize(NetworkWriter writer, NetworkMessage instance)
        {
            instance.Serialize(writer);
        }

        public override NetworkMessage Deserialize(NetworkReader reader)
        {
            var message = new NetworkMessage();

            message.Deserialize(reader);

            return message;
        }
    }

    #region POCO
    [Preserve]
    public class GuidNetworkSerializationResolver : NetworkSerializationExplicitResolver<Guid>
    {
        public const byte Size = 16;

        public override void Serialize(NetworkWriter writer, Guid instance)
        {
            var binary = instance.ToByteArray();

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
        public override void Serialize(NetworkWriter writer, DateTime instance)
        {
            long binary = instance.ToBinary();

            writer.Write(binary);
        }

        public override DateTime Deserialize(NetworkReader reader)
        {
            reader.Read(out long binary);

            return DateTime.FromBinary(binary);
        }
    }

    [Preserve]
    public class TimeSpanNetworkSerializationResolver : NetworkSerializationExplicitResolver<TimeSpan>
    {
        public override void Serialize(NetworkWriter writer, TimeSpan instance)
        {
            long ticks = instance.Ticks;

            writer.Write(ticks);
        }

        public override TimeSpan Deserialize(NetworkReader reader)
        {
            reader.Read(out long ticks);

            return TimeSpan.FromTicks(ticks);
        }
    }

    [Preserve]
    public class IPAddressNetworkSerializationResolver : NetworkSerializationExplicitResolver<IPAddress>
    {
        public override void Serialize(NetworkWriter writer, IPAddress instance)
        {
            if(instance == null)
            {
                writer.Insert(0);
                return;
            }

            var bytes = instance.GetAddressBytes();

            byte length = (byte)bytes.Length;

            writer.Insert(length);
            writer.Insert(bytes);
        }

        public override IPAddress Deserialize(NetworkReader reader)
        {
            var length = reader.Next();

            if (length == 0) return null;

            var binary = reader.BlockCopy(length);

            var value = new IPAddress(binary);

            return value;
        }
    }
    #endregion

    [Preserve]
    public sealed class ByteArrayNetworkSerializationResolver : NetworkSerializationExplicitResolver<byte[]>
    {
        public override void Serialize(NetworkWriter writer, byte[] instance)
        {
            writer.Write(instance.Length);

            writer.Insert(instance);
        }

        public override byte[] Deserialize(NetworkReader reader)
        {
            reader.Read(out int length);

            var value = reader.BlockCopy(length);

            return value;
        }
    }
    #endregion

    #region Generic
    [Preserve]
    public abstract class NetworkSerializationGenericResolver<T> : NetworkSerializationExplicitResolver<T>
    {
        public NetworkSerializationGenericResolver()
        {
            Instance = this;
        }

        static NetworkSerializationGenericResolver()
        {
            //Explicitly Called to make sure that the base class's static constructor is called
            Initialiaze();
        }
    }

    [Preserve]
    public sealed class INetworkSerializableResolver<T> : NetworkSerializationGenericResolver<T>
        where T : INetworkSerializable, new()
    {
        bool nullable;

        public override void Serialize(NetworkWriter writer, T instance)
        {
            if (nullable) if (WriteNull(writer, instance)) return;

            var context = new NetworkSerializationContext(writer);

            instance.Select(ref context);
        }

        public override T Deserialize(NetworkReader reader)
        {
            if (nullable) if (ReadNull(reader)) return default;

            var value = new T();

            var context = new NetworkSerializationContext(reader);

            value.Select(ref context);

            return value;
        }

        public INetworkSerializableResolver()
        {
            nullable = NetworkSerializationHelper.Nullable.Generic<T>.Is;
        }
    }

    [Preserve]
    public sealed class IManualNetworkSerializableResolver<T> : NetworkSerializationGenericResolver<T>
        where T : IManualNetworkSerializable, new()
    {
        bool nullable;

        public override void Serialize(NetworkWriter writer, T instance)
        {
            if (nullable) if (WriteNull(writer, instance)) return;

            instance.Serialize(writer);
        }

        public override T Deserialize(NetworkReader reader)
        {
            if (nullable) if (ReadNull(reader)) return default;

            var value = new T();

            value.Deserialize(reader);

            return value;
        }

        public IManualNetworkSerializableResolver()
        {
            nullable = NetworkSerializationHelper.Nullable.Generic<T>.Is;
        }
    }

    [Preserve]
    public sealed class EnumNetworkSerializationResolver<TType, TBacking> : NetworkSerializationGenericResolver<TType>
        where TType : struct, Enum
    {
        ConcurrentDictionary<TBacking, TType> values;
        ConcurrentDictionary<TType, TBacking> backings;

        public override void Serialize(NetworkWriter writer, TType instance)
        {
            if (backings.TryGetValue(instance, out var backing) == false)
            {
                backing = ChangeType<TBacking>(instance);
                Register(backing, instance);
            }

            writer.Write(backing);
        }

        public override TType Deserialize(NetworkReader reader)
        {
            var backing = reader.Read<TBacking>();

            if (values.TryGetValue(backing, out var instance) == false)
            {
                instance = ToEnum<TType>(backing);
                Register(backing, instance);
            }

            return instance;
        }

        void Register(TBacking backing, TType type)
        {
            values.TryAdd(backing, type);
            backings.TryAdd(type, backing);
        }

        public static T ToEnum<T>(object source) => (T)Enum.ToObject(typeof(T), source);
        public static T ChangeType<T>(object source) => (T)Convert.ChangeType(source, typeof(T));

        public EnumNetworkSerializationResolver()
        {
            values = new ConcurrentDictionary<TBacking, TType>();
            backings = new ConcurrentDictionary<TType, TBacking>();

            foreach (TType instance in Enum.GetValues(typeof(TType)))
            {
                var backing = ChangeType<TBacking>(instance);

                Register(backing, instance);
            }
        }
    }

    [Preserve]
    public sealed class NullableNetworkSerializationResolver<TData> : NetworkSerializationGenericResolver<TData?>
        where TData : struct
    {
        public override void Serialize(NetworkWriter writer, TData? instance)
        {
            if (WriteNull(writer, instance)) return;

            writer.Write(instance.Value);
        }

        public override TData? Deserialize(NetworkReader reader)
        {
            if (ReadNull(reader)) return null;

            return reader.Read<TData>();
        }
    }

    #region Tuple
    [Preserve]
    public sealed class TupleNetwokrSerializationResolver<T1, T2> : NetworkSerializationGenericResolver<(T1, T2)>
    {
        public override void Serialize(NetworkWriter writer, (T1, T2) instance)
        {
            writer.Write(instance.Item1);
            writer.Write(instance.Item2);
        }

        public override (T1, T2) Deserialize(NetworkReader reader)
        {
            reader.Read(out T1 arg1);
            reader.Read(out T2 arg2);

            return (arg1, arg2);
        }
    }

    [Preserve]
    public sealed class TupleNetwokrSerializationResolver<T1, T2, T3> : NetworkSerializationGenericResolver<(T1, T2, T3)>
    {
        public override void Serialize(NetworkWriter writer, (T1, T2, T3) instance)
        {
            writer.Write(instance.Item1);
            writer.Write(instance.Item2);
            writer.Write(instance.Item3);
        }

        public override (T1, T2, T3) Deserialize(NetworkReader reader)
        {
            reader.Read(out T1 arg1);
            reader.Read(out T2 arg2);
            reader.Read(out T3 arg3);

            return (arg1, arg2, arg3);
        }
    }

    [Preserve]
    public sealed class TupleNetwokrSerializationResolver<T1, T2, T3, T4> : NetworkSerializationGenericResolver<(T1, T2, T3, T4)>
    {
        public override void Serialize(NetworkWriter writer, (T1, T2, T3, T4) instance)
        {
            writer.Write(instance.Item1);
            writer.Write(instance.Item2);
            writer.Write(instance.Item3);
            writer.Write(instance.Item4);
        }

        public override (T1, T2, T3, T4) Deserialize(NetworkReader reader)
        {
            reader.Read(out T1 arg1);
            reader.Read(out T2 arg2);
            reader.Read(out T3 arg3);
            reader.Read(out T4 arg4);

            return (arg1, arg2, arg3, arg4);
        }
    }

    [Preserve]
    public sealed class TupleNetwokrSerializationResolver<T1, T2, T3, T4, T5> : NetworkSerializationGenericResolver<(T1, T2, T3, T4, T5)>
    {
        public override void Serialize(NetworkWriter writer, (T1, T2, T3, T4, T5) instance)
        {
            writer.Write(instance.Item1);
            writer.Write(instance.Item2);
            writer.Write(instance.Item3);
            writer.Write(instance.Item4);
            writer.Write(instance.Item5);
        }

        public override (T1, T2, T3, T4, T5) Deserialize(NetworkReader reader)
        {
            reader.Read(out T1 arg1);
            reader.Read(out T2 arg2);
            reader.Read(out T3 arg3);
            reader.Read(out T4 arg4);
            reader.Read(out T5 arg5);

            return (arg1, arg2, arg3, arg4, arg5);
        }
    }

    [Preserve]
    public sealed class TupleNetwokrSerializationResolver<T1, T2, T3, T4, T5, T6> : NetworkSerializationGenericResolver<(T1, T2, T3, T4, T5, T6)>
    {
        public override void Serialize(NetworkWriter writer, (T1, T2, T3, T4, T5, T6) instance)
        {
            writer.Write(instance.Item1);
            writer.Write(instance.Item2);
            writer.Write(instance.Item3);
            writer.Write(instance.Item4);
            writer.Write(instance.Item5);
            writer.Write(instance.Item6);
        }

        public override (T1, T2, T3, T4, T5, T6) Deserialize(NetworkReader reader)
        {
            reader.Read(out T1 arg1);
            reader.Read(out T2 arg2);
            reader.Read(out T3 arg3);
            reader.Read(out T4 arg4);
            reader.Read(out T5 arg5);
            reader.Read(out T6 arg6);

            return (arg1, arg2, arg3, arg4, arg5, arg6);
        }
    }

    [Preserve]
    public sealed class TupleNetwokrSerializationResolver<T1, T2, T3, T4, T5, T6, T7> : NetworkSerializationGenericResolver<(T1, T2, T3, T4, T5, T6, T7)>
    {
        public override void Serialize(NetworkWriter writer, (T1, T2, T3, T4, T5, T6, T7) instance)
        {
            writer.Write(instance.Item1);
            writer.Write(instance.Item2);
            writer.Write(instance.Item3);
            writer.Write(instance.Item4);
            writer.Write(instance.Item5);
            writer.Write(instance.Item6);
            writer.Write(instance.Item7);
        }

        public override (T1, T2, T3, T4, T5, T6, T7) Deserialize(NetworkReader reader)
        {
            reader.Read(out T1 arg1);
            reader.Read(out T2 arg2);
            reader.Read(out T3 arg3);
            reader.Read(out T4 arg4);
            reader.Read(out T5 arg5);
            reader.Read(out T6 arg6);
            reader.Read(out T7 arg7);

            return (arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
    }

    [Preserve]
    public sealed class TupleNetwokrSerializationResolver<T1, T2, T3, T4, T5, T6, T7, T8> : NetworkSerializationGenericResolver<(T1, T2, T3, T4, T5, T6, T7, T8)>
    {
        public override void Serialize(NetworkWriter writer, (T1, T2, T3, T4, T5, T6, T7, T8) instance)
        {
            writer.Write(instance.Item1);
            writer.Write(instance.Item2);
            writer.Write(instance.Item3);
            writer.Write(instance.Item4);
            writer.Write(instance.Item5);
            writer.Write(instance.Item6);
            writer.Write(instance.Item7);
            writer.Write(instance.Item8);
        }

        public override (T1, T2, T3, T4, T5, T6, T7, T8) Deserialize(NetworkReader reader)
        {
            reader.Read(out T1 arg1);
            reader.Read(out T2 arg2);
            reader.Read(out T3 arg3);
            reader.Read(out T4 arg4);
            reader.Read(out T5 arg5);
            reader.Read(out T6 arg6);
            reader.Read(out T7 arg7);
            reader.Read(out T8 arg8);

            return (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
    }
    #endregion

    #region Collections
    [Preserve]
    public sealed class ArrayNetworkSerializationResolver<TElement> : NetworkSerializationGenericResolver<TElement[]>
    {
        public override void Serialize(NetworkWriter writer, TElement[] instance)
        {
            if (WriteNull(writer, instance)) return;

            NetworkSerializationHelper.Length.Write(writer, instance.Length);

            for (int i = 0; i < instance.Length; i++) writer.Write(instance[i]);
        }

        public override TElement[] Deserialize(NetworkReader reader)
        {
            if (ReadNull(reader)) return null;

            NetworkSerializationHelper.Length.Read(reader, out var length);

            var array = new TElement[length];

            for (int i = 0; i < length; i++) array[i] = reader.Read<TElement>();

            return array;
        }
    }

    [Preserve]
    public sealed class ArraySegmentNetworkSerializationResolver<TElement> : NetworkSerializationGenericResolver<ArraySegment<TElement>>
    {
        public override void Serialize(NetworkWriter writer, ArraySegment<TElement> instance)
        {
            NetworkSerializationHelper.Length.Write(writer, instance.Count);

            for (int i = 0; i < instance.Count; i++)
            {
                var element = instance.Array[instance.Offset + i];

                writer.Write(element);
            }
        }

        public override ArraySegment<TElement> Deserialize(NetworkReader reader)
        {
            NetworkSerializationHelper.Length.Read(reader, out var length);

            var array = new TElement[length];

            for (int i = 0; i < length; i++) array[i] = reader.Read<TElement>();

            var segment = new ArraySegment<TElement>(array);

            return segment;
        }
    }

    [Preserve]
    public sealed class ListNetworkSerializationResolver<TElement> : NetworkSerializationGenericResolver<List<TElement>>
    {
        public override void Serialize(NetworkWriter writer, List<TElement> instance)
        {
            if (WriteNull(writer, instance)) return;

            NetworkSerializationHelper.Length.Write(writer, instance.Count);

            for (int i = 0; i < instance.Count; i++) writer.Write(instance[i]);
        }

        public override List<TElement> Deserialize(NetworkReader reader)
        {
            if (ReadNull(reader)) return null;

            NetworkSerializationHelper.Length.Read(reader, out var count);

            var list = new List<TElement>(count);

            for (int i = 0; i < count; i++)
            {
                var element = reader.Read<TElement>();

                list.Add(element);
            }

            return list;
        }
    }

    [Preserve]
    public sealed class HashSetNetworkSerializationResolver<TElement> : NetworkSerializationGenericResolver<HashSet<TElement>>
    {
        public override void Serialize(NetworkWriter writer, HashSet<TElement> instance)
        {
            if (WriteNull(writer, instance)) return;

            NetworkSerializationHelper.Length.Write(writer, instance.Count);

            foreach (var item in instance) writer.Write(item);
        }

        public override HashSet<TElement> Deserialize(NetworkReader reader)
        {
            if (ReadNull(reader)) return null;

            NetworkSerializationHelper.Length.Read(reader, out var length);

            var array = new TElement[length];

            for (int i = 0; i < length; i++) array[i] = reader.Read<TElement>();

            return new HashSet<TElement>(array);
            //Hashset doesn't expose a capacity constructor, that's why I'm creating it from an array
        }
    }

    [Preserve]
    public sealed class DictionaryNetworkSerializationResolver<TKey, TValue> : NetworkSerializationGenericResolver<Dictionary<TKey, TValue>>
    {
        public override void Serialize(NetworkWriter writer, Dictionary<TKey, TValue> instance)
        {
            if (WriteNull(writer, instance)) return;

            NetworkSerializationHelper.Length.Write(writer, instance.Count);

            foreach (var pair in instance)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }

        public override Dictionary<TKey, TValue> Deserialize(NetworkReader reader)
        {
            if (ReadNull(reader)) return null;

            NetworkSerializationHelper.Length.Read(reader, out var count);

            var dictionary = new Dictionary<TKey, TValue>(count);

            for (int i = 0; i < count; i++)
            {
                var key = reader.Read<TKey>();
                var value = reader.Read<TValue>();

                dictionary.Add(key, value);
            }

            return dictionary;
        }
    }
    #endregion
    #endregion

    #region Implicit
    [Preserve]
    public abstract class NetworkSerializationImplicitResolver : NetworkSerializationResolver
    {
        //Static

        static NetworkSerializationImplicitResolver()
        {
            //Explicitly Called to make sure that the base class's static constructor is called
            Initialiaze();
        }
    }

    [Preserve]
    public sealed class INetworkSerializableResolver : NetworkSerializationImplicitResolver
    {
        public static Type Interface { get; } = typeof(INetworkSerializable);
        public static bool IsValid(Type target) => Interface.IsAssignableFrom(target);

        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            if (WriteNull(writer, instance, type)) return;

            var value = instance as INetworkSerializable;

            var context = new NetworkSerializationContext(writer);

            value.Select(ref context);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (ReadNull(reader, type)) return null;

            var value = Activator.CreateInstance(type, true) as INetworkSerializable;

            var context = new NetworkSerializationContext(reader);

            value.Select(ref context);

            return value;
        }
    }

    [Preserve]
    public sealed class IManualNetworkSerializableResolver : NetworkSerializationImplicitResolver
    {
        public static Type Interface { get; } = typeof(IManualNetworkSerializable);
        public static bool IsValid(Type target) => Interface.IsAssignableFrom(target);

        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            if (WriteNull(writer, instance, type)) return;

            var value = instance as IManualNetworkSerializable;

            value.Serialize(writer);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (ReadNull(reader, type)) return null;

            var value = Activator.CreateInstance(type, true) as IManualNetworkSerializable;

            value.Deserialize(reader);

            return value;
        }
    }

    [Preserve]
    public sealed class TupleNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static bool IsValid(Type target) => TupleUtility.CheckType(target);

        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            if (WriteNull(writer, instance, type)) return;

            var values = TupleUtility.Extract(instance);

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type[] arguments);

            for (int i = 0; i < values.Length; i++)
                writer.Write(values[i], arguments[i]);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (ReadNull(reader, type)) return null;

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type[] arguments);

            var items = new object[arguments.Length];

            for (int i = 0; i < arguments.Length; i++)
                items[i] = reader.Read(arguments[i]);

            var value = Activator.CreateInstance(type, items);

            return value;
        }
    }

    [Preserve]
    public sealed class NullableNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static bool IsValid(Type target)
        {
            if (target.IsGenericType == false) return false;

            return target.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            if (WriteNull(writer, instance)) return;

            writer.Write(instance);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (ReadNull(reader)) return null;

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type underlying);

            var value = reader.Read(underlying);

            return Activator.CreateInstance(type, value);
        }
    }

    [Preserve]
    public sealed class EnumNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static bool IsValid(Type target) => target.IsEnum;

        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            var value = NetworkSerializationHelper.Enum.Value.Retrieve(instance);

            writer.Write(value);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            var backing = NetworkSerializationHelper.Enum.UnderlyingType.Retrieve(type);

            var value = reader.Read(backing);

            var result = Enum.ToObject(type, value);

            return result;
        }
    }

    #region Collection
    [Preserve]
    public sealed class ArrayNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static bool IsValid(Type target) => target.IsArray;

        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            if (WriteNull(writer, instance)) return;

            var array = instance as IList;

            NetworkSerializationHelper.Length.Write(writer, array.Count);

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type argument);

            for (int i = 0; i < array.Count; i++) writer.Write(array[i], argument);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (ReadNull(reader)) return null;

            NetworkSerializationHelper.Length.Read(reader, out var length);

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type argument);

            var array = Array.CreateInstance(argument, length);

            for (int i = 0; i < length; i++)
            {
                var element = reader.Read(argument);

                array.SetValue(element, i);
            }

            return array;
        }
    }

    [Preserve]
    public sealed class ArraySegmentNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static bool IsValid(Type target)
        {
            if (target.IsGenericType == false) return false;

            return target.GetGenericTypeDefinition() == typeof(ArraySegment<>);
        }

        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            if (WriteNull(writer, instance)) return;

            var value = instance as IEnumerable;

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type argument);

            //TODO find a way to get the count to optimize this surrogate creation
            var list = NetworkSerializationHelper.List.Instantiate(argument, 0);

            foreach (var item in value) list.Add(item);

            NetworkSerializationHelper.Length.Write(writer, list.Count);

            for (int i = 0; i < list.Count; i++) writer.Write(list[i], argument);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (ReadNull(reader)) return null;

            NetworkSerializationHelper.Length.Read(reader, out var count);

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type argument);

            var array = Array.CreateInstance(argument, count) as IList;

            for (int i = 0; i < count; i++)
                array[i] = reader.Read(argument);

            var segment = Activator.CreateInstance(type, array);

            return segment;
        }
    }

    [Preserve]
    public sealed class ListNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static bool IsValid(Type target)
        {
            if (target.IsGenericType == false) return false;

            return target.GetGenericTypeDefinition() == typeof(List<>);
        }

        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            if (WriteNull(writer, instance)) return;

            var list = instance as IList;

            NetworkSerializationHelper.Length.Write(writer, list.Count);

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type argument);

            for (int i = 0; i < list.Count; i++) writer.Write(list[i], argument);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (ReadNull(reader)) return null;

            NetworkSerializationHelper.Length.Read(reader, out var count);

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type argument);

            var list = NetworkSerializationHelper.List.Instantiate(argument, count);

            for (int i = 0; i < count; i++)
            {
                var element = reader.Read(argument);

                list.Add(element);
            }

            return list;
        }
    }

    [Preserve]
    public sealed class HashSetNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static bool IsValid(Type target)
        {
            if (target.IsGenericType == false) return false;

            return target.GetGenericTypeDefinition() == typeof(HashSet<>);
        }

        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            if (WriteNull(writer, instance)) return;

            var value = instance as IEnumerable;

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type argument);

            //TODO find a way to get the count to optimize this surrogate creation
            var list = NetworkSerializationHelper.List.Instantiate(argument, 0);

            foreach (var item in value) list.Add(item);

            NetworkSerializationHelper.Length.Write(writer, list.Count);

            for (int i = 0; i < list.Count; i++) writer.Write(list[i], argument);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (ReadNull(reader)) return null;

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out Type argument);

            NetworkSerializationHelper.Length.Read(reader, out var length);

            var array = Array.CreateInstance(argument, length) as IList;

            for (int i = 0; i < length; i++)
            {
                var element = reader.Read(argument);

                array[i] = element;
            }

            return Activator.CreateInstance(type, array);
        }
    }

    [Preserve]
    public sealed class DictionaryNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static bool IsValid(Type target)
        {
            if (target.IsGenericType == false) return false;

            return target.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            if (WriteNull(writer, instance)) return;

            var dictionary = (IDictionary)instance;

            NetworkSerializationHelper.Length.Write(writer, dictionary.Count);

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out var keyType, out var valueType);

            foreach (DictionaryEntry entry in dictionary)
            {
                writer.Write(entry.Key, keyType);
                writer.Write(entry.Value, valueType);
            }
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (ReadNull(reader)) return null;

            NetworkSerializationHelper.Length.Read(reader, out var count);

            var dictionary = Activator.CreateInstance(type, count) as IDictionary;

            NetworkSerializationHelper.GenericArguments.Retrieve(type, out var keyType, out var valueType);

            for (int i = 0; i < count; i++)
            {
                var key = reader.Read(keyType);
                var value = reader.Read(valueType);

                dictionary.Add(key, value);
            }

            return dictionary;
        }
    }
    #endregion
    #endregion

    [Preserve]
    public struct NetworkSerializationContext
    {
        public NetworkWriter Writer { get; private set; }
        public bool IsSerializing => Writer != null;

        public NetworkReader Reader { get; private set; }
        public bool IsDeserializing => Reader != null;

        public void Select<T>(ref T value)
        {
            if (IsSerializing) Writer.Write(value);

            if (IsDeserializing) Reader.Read(out value);
        }

        NetworkSerializationContext(NetworkWriter writer, NetworkReader reader)
        {
            this.Writer = writer;
            this.Reader = reader;
        }

        public NetworkSerializationContext(NetworkWriter writer) : this(writer, null) { }
        public NetworkSerializationContext(NetworkReader reader) : this(null, reader) { }
    }
}