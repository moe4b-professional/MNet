﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

using Helper = MNet.NetworkSerializationHelper;

namespace MNet
{
    #region Resolvers
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

            if (Helper.Nullable.Any.Check(type)) writer.Insert(0); //Is Not Null Flag

            return false;
        }

        public abstract object Deserialize(NetworkReader reader, Type type);

        protected static bool ReadNull(NetworkReader reader)
        {
            return reader.TakeByte() == 1 ? true : false;
        }
        protected static bool ReadNull(NetworkReader reader, Type type)
        {
            if (Helper.Nullable.Any.Check(type) == false) return false;

            return ReadNull(reader);
        }

        public NetworkSerializationResolver() { }

        //Static Utility

        public static List<NetworkSerializationResolver> Implicits { get; private set; }
        public static List<NetworkSerializationResolver> Explicits { get; private set; }

        public static Dictionary<Type, NetworkSerializationResolver> Dictionary { get; private set; }

        protected static readonly object StaticSyncLock = new object();

        static bool TryFindExplicit(Type type, out NetworkSerializationResolver resolver)
        {
            for (int i = 0; i < Explicits.Count; i++)
            {
                if (Explicits[i].CanResolve(type))
                {
                    resolver = Explicits[i];
                    return true;
                }
            }

            resolver = default;
            return false;
        }
        static bool TryFindImplicit(Type type, out NetworkSerializationResolver resolver)
        {
            for (int i = 0; i < Implicits.Count; i++)
            {
                if (Implicits[i].CanResolve(type))
                {
                    resolver = Implicits[i];
                    return true;
                }
            }

            resolver = default;
            return false;
        }

        public static NetworkSerializationExplicitResolver<T> Retrieve<T>()
        {
            lock (StaticSyncLock)
            {
                if (NetworkSerializationExplicitResolver<T>.Instance != null)
                    return NetworkSerializationExplicitResolver<T>.Instance;

                DynamicNetworkSerialization.Resolve(typeof(T), out var resolver);

                return NetworkSerializationExplicitResolver<T>.Instance;
            }
        }

        public static NetworkSerializationResolver Retrive(Type type)
        {
            lock (StaticSyncLock)
            {
                if (Dictionary.TryGetValue(type, out var resolver))
                    return resolver;

                if (TryFindExplicit(type, out resolver))
                {
                    Assign(type, resolver);
                }
                else if (DynamicNetworkSerialization.Resolve(type, out resolver))
                {
                    Assign(type, resolver);
                }
                else if (TryFindImplicit(type, out resolver))
                {
                    Assign(type, resolver);

                    if (DynamicNetworkSerialization.Enabled)
                    {
                        Log.Warning($"No Dynamic or Explicit Serialization Resolver Found for ({type}), But an Implicit Resolver was Found" +
                               $", Please Consider Writing a Dynamic Resolver To Improve Serialization Speed on JIT Platforms");
                    }
                }

                static void Assign(Type type, NetworkSerializationResolver resolver)
                {
                    Dictionary.Add(type, resolver);
                }

                return resolver;
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

    [Preserve]
    public abstract class NetworkSerializationExplicitResolver<T> : NetworkSerializationResolver
    {
        internal static NetworkSerializationExplicitResolver<T> Instance;

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
                throw new InvalidCastException($"Cannot Use ({GetType()}) to Serialize ({type}) Explicitly, Because ({type}) Cannot be cast to {typeof(T)}");
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
    }

    [Preserve]
    public abstract class NetworkSerializationImplicitResolver : NetworkSerializationResolver
    {

    }

    [Preserve]
    public abstract class NetworkSerializationDynamicResolver<T> : NetworkSerializationExplicitResolver<T>
    {
        public NetworkSerializationDynamicResolver() : base()
        {

        }
    }
    #endregion

    [Preserve]
    public static class DynamicNetworkSerialization
    {
        public static bool Enabled { get; set; } = true;

        public static List<ResolveDelegate> Resolvers { get; private set; }
        public delegate bool ResolveDelegate(Type type, ref NetworkSerializationResolver resolver);

        internal static bool Resolve(Type type, out NetworkSerializationResolver resolver)
        {
            resolver = default;

            if (Enabled == false)
                return false;

            for (int i = 0; i < Resolvers.Count; i++)
                if (Resolvers[i].Invoke(type, ref resolver))
                    return true;

            return false;
        }

        public static void Register(ResolveDelegate method) => Resolvers.Add(method);

        static DynamicNetworkSerialization()
        {
            Resolvers = new List<ResolveDelegate>();

            Register(ResolveINetworkSerilizable);
            Register(ResolveIManualNetworkSerilizable);

            Register(ResolveEnum);

            Register(ResolveNullable);

            Register(ResolveTuple);

            Register(ResolveArray);
            Register(ResolveArraySegment);
            Register(ResolveList);
            Register(ResolveHashSet);
            Register(ResolveQueue);
            Register(ResolveStack);
            Register(ResolveDictionary);
        }

        #region Default Resolvers
        static bool ResolveINetworkSerilizable(Type type, ref NetworkSerializationResolver resolver)
        {
            if (INetworkSerializableResolver.IsValid(type) == false)
                return false;

            resolver = ConstructResolver(typeof(INetworkSerializableResolver<>), type);
            return true;
        }

        static bool ResolveIManualNetworkSerilizable(Type type, ref NetworkSerializationResolver resolver)
        {
            if (IManualNetworkSerializableResolver.IsValid(type) == false)
                return false;

            resolver = ConstructResolver(typeof(IManualNetworkSerializableResolver<>), type);
            return true;
        }

        static bool ResolveEnum(Type type, ref NetworkSerializationResolver resolver)
        {
            if (EnumNetworkSerializationResolver.IsValid(type) == false)
                return false;

            var backing = Helper.Enum.UnderlyingType.Retrieve(type);

            resolver = ConstructResolver(typeof(EnumNetworkSerializationResolver<,>), type, backing);
            return true;
        }

        static bool ResolveNullable(Type type, ref NetworkSerializationResolver resolver)
        {
            if (NullableNetworkSerializationResolver.IsValid(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(NullableNetworkSerializationResolver<>), argument);
            return true;
        }

        static bool ResolveTuple(Type type, ref NetworkSerializationResolver resolver)
        {
            if (type.IsValueType == false)
                return false;

            if (TupleNetworkSerializationResolver.IsValid(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type[] arguments);

            switch (arguments.Length)
            {
                case 2:
                    resolver = ConstructResolver(typeof(TupleNetworkSerializationResolver<,>), arguments);
                    return true;

                case 3:
                    resolver = ConstructResolver(typeof(TupleNetworkSerializationResolver<,,>), arguments);
                    return true;

                case 4:
                    resolver = ConstructResolver(typeof(TupleNetworkSerializationResolver<,,,>), arguments);
                    return true;

                case 5:
                    resolver = ConstructResolver(typeof(TupleNetworkSerializationResolver<,,,,>), arguments);
                    return true;

                case 6:
                    resolver = ConstructResolver(typeof(TupleNetworkSerializationResolver<,,,,,>), arguments);
                    return true;

                case 7:
                    resolver = ConstructResolver(typeof(TupleNetworkSerializationResolver<,,,,,,>), arguments);
                    return true;

                case 8:
                    resolver = ConstructResolver(typeof(TupleNetworkSerializationResolver<,,,,,,>), arguments);
                    return true;
            }

            return false;
        }

        static bool ResolveArray(Type type, ref NetworkSerializationResolver resolver)
        {
            if (ArrayNetworkSerializationResolver.IsValid(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(ArrayNetworkSerializationResolver<>), argument);
            return true;
        }

        static bool ResolveArraySegment(Type type, ref NetworkSerializationResolver resolver)
        {
            if (ArraySegmentNetworkSerializationResolver.IsValid(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(ArraySegmentNetworkSerializationResolver<>), argument);
            return true;
        }

        static bool ResolveList(Type type, ref NetworkSerializationResolver resolver)
        {
            if (ListNetworkSerializationResolver.IsValid(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(ListNetworkSerializationResolver<>), argument);
            return true;
        }

        static bool ResolveHashSet(Type type, ref NetworkSerializationResolver resolver)
        {
            if (HashSetNetworkSerializationResolver.IsValid(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(HashSetNetworkSerializationResolver<>), argument);
            return true;
        }

        static bool ResolveQueue(Type type, ref NetworkSerializationResolver resolver)
        {
            if (QueueNetworkSerializationResolver.IsValid(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(QueueNetworkSerializationResolver<>), argument);
            return true;
        }

        static bool ResolveStack(Type type, ref NetworkSerializationResolver resolver)
        {
            if (StackNetworkSerializationResolver.IsValid(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(StackNetworkSerializationResolver<>), argument);
            return true;
        }

        static bool ResolveDictionary(Type type, ref NetworkSerializationResolver resolver)
        {
            if (DictionaryNetworkSerializationResolver.IsValid(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type key, out Type value);

            resolver = ConstructResolver(typeof(DictionaryNetworkSerializationResolver<,>), key, value);
            return true;
        }
        #endregion

        //Utility

        public static NetworkSerializationResolver ConstructResolver(Type definition, params Type[] arguments)
        {
            if (typeof(NetworkSerializationResolver).IsAssignableFrom(definition) == false)
                throw new ArgumentException($"Cannot use Type ({definition}) to Create a Network Serialization Resolver");

            var target = definition.MakeGenericType(arguments);

            var resolver = Activator.CreateInstance(target) as NetworkSerializationResolver;

            return resolver;
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
            return reader.TakeByte();
        }
    }
    [Preserve]
    public sealed class SByteNetworkSerializationResolver : NetworkSerializationExplicitResolver<sbyte>
    {
        public override void Serialize(NetworkWriter writer, sbyte instance)
        {
            writer.Insert((byte)instance);
        }
        public override sbyte Deserialize(NetworkReader reader)
        {
            return (sbyte)reader.TakeByte();
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
            var value = reader.TakeByte();

            return value == 0 ? false : true;
        }
    }

    [Preserve]
    public sealed class ShortNetworkSerializationResolver : NetworkSerializationExplicitResolver<short>
    {
        public override void Serialize(NetworkWriter writer, short instance)
        {
            Span<byte> span = stackalloc byte[sizeof(short)];

            if (BitConverter.TryWriteBytes(span, instance) == false)
                throw new InvalidOperationException($"Couldn't Convert to Binary");

            writer.Insert(span);
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
            Span<byte> span = stackalloc byte[sizeof(ushort)];

            if (BitConverter.TryWriteBytes(span, instance) == false)
                throw new InvalidOperationException($"Couldn't Convert to Binary");

            writer.Insert(span);
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
            Span<byte> span = stackalloc byte[sizeof(int)];

            if (BitConverter.TryWriteBytes(span, instance) == false)
                throw new InvalidOperationException($"Couldn't Convert to Binary");

            writer.Insert(span);
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
            Span<byte> span = stackalloc byte[sizeof(uint)];

            if (BitConverter.TryWriteBytes(span, instance) == false)
                throw new InvalidOperationException($"Couldn't Convert to Binary");

            writer.Insert(span);
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
            Span<byte> span = stackalloc byte[sizeof(long)];

            if (BitConverter.TryWriteBytes(span, instance) == false)
                throw new InvalidOperationException($"Couldn't Convert to Binary");

            writer.Insert(span);
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
            Span<byte> span = stackalloc byte[sizeof(ulong)];

            if (BitConverter.TryWriteBytes(span, instance) == false)
                throw new InvalidOperationException($"Couldn't Convert to Binary");

            writer.Insert(span);
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
            Span<byte> span = stackalloc byte[sizeof(float)];

            if (BitConverter.TryWriteBytes(span, instance) == false)
                throw new InvalidOperationException($"Couldn't Convert to Binary");

            writer.Insert(span);
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
            Span<byte> span = stackalloc byte[sizeof(double)];

            if (BitConverter.TryWriteBytes(span, instance) == false)
                throw new InvalidOperationException($"Couldn't Convert to Binary");

            writer.Insert(span);
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
        public const int MaxStackAllocationSize = 1024;

        public override void Serialize(NetworkWriter writer, string instance)
        {
            if (instance == null)
            {
                Helper.Length.Collection.WriteNull(writer);
            }
            else if (instance.Length == 0)
            {
                Helper.Length.Collection.WriteValue(writer, 0);
            }
            else
            {
                var size = Encoding.UTF8.GetByteCount(instance);

                Span<byte> span = size > MaxStackAllocationSize ? new byte[size] : stackalloc byte[size];

                Encoding.UTF8.GetBytes(instance, span);

                Helper.Length.Collection.WriteValue(writer, size);
                writer.Insert(span);
            }
        }
        public override string Deserialize(NetworkReader reader)
        {
            if (Helper.Length.Collection.Read(reader, out var length) == false)
                return null;

            if (length == 0)
                return string.Empty;

            var value = Encoding.UTF8.GetString(reader.Data, reader.Position, length);

            reader.Position += length;

            return value;
        }
    }
    #endregion

    #region POCO
    [Preserve]
    public class GuidNetworkSerializationResolver : NetworkSerializationExplicitResolver<Guid>
    {
        public const byte Size = 16;

        public override void Serialize(NetworkWriter writer, Guid instance)
        {
            Span<byte> span = stackalloc byte[Size];

            if (instance.TryWriteBytes(span) == false)
                throw new InvalidOperationException($"Couldn't Convert to Binary");

            writer.Insert(span);
        }
        public override Guid Deserialize(NetworkReader reader)
        {
            var span = reader.TakeSpan(Size);

            var value = new Guid(span);

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
        public const int MaxSize = 16;

        public override void Serialize(NetworkWriter writer, IPAddress instance)
        {
            if (instance == null)
            {
                writer.Insert(0);
                return;
            }

            Span<byte> span = stackalloc byte[MaxSize];

            if (instance.TryWriteBytes(span, out var length) == false)
                throw new InvalidOperationException($"Couldn't Convert to Binary");

            span = span.Slice(0, length);

            writer.Insert((byte)length);
            writer.Insert(span);
        }
        public override IPAddress Deserialize(NetworkReader reader)
        {
            var length = reader.TakeByte();

            if (length == 0)
                return null;

            var span = reader.TakeSpan(length);

            var value = new IPAddress(span);

            return value;
        }
    }

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
            var code = reader.TakeByte();

            var value = NetworkPayload.GetType(code);

            return value;
        }
    }
    #endregion

    #region Array
    [Preserve]
    public sealed class ArrayNetworkSerializationResolver<TElement> : NetworkSerializationDynamicResolver<TElement[]>
    {
        public override void Serialize(NetworkWriter writer, TElement[] instance)
        {
            if (Helper.Length.Collection.WriteGeneric(writer, instance) == false) return;

            for (int i = 0; i < instance.Length; i++) writer.Write(instance[i]);
        }
        public override TElement[] Deserialize(NetworkReader reader)
        {
            if (Helper.Length.Collection.Read(reader, out var length) == false) return null;

            var array = new TElement[length];

            for (int i = 0; i < length; i++) array[i] = reader.Read<TElement>();

            return array;
        }
    }

    [Preserve]
    public sealed class ArrayNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static bool IsValid(Type target)
        {
            if (target.IsArray == false)
                return false;

            if (target.GetArrayRank() != 1)
                return false;

            return true;
        }
        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            var array = instance as IList;

            if (Helper.Length.Collection.WriteExplicit(writer, array) == false) return;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            for (int i = 0; i < array.Count; i++) writer.Write(array[i], argument);
        }
        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (Helper.Length.Collection.Read(reader, out var length) == false) return null;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            var array = Array.CreateInstance(argument, length);

            for (int i = 0; i < length; i++)
            {
                var element = reader.Read(argument);

                array.SetValue(element, i);
            }

            return array;
        }
    }
    #endregion

    #region Array Segment
    [Preserve]
    public sealed class ArraySegmentNetworkSerializationResolver<TElement> : NetworkSerializationDynamicResolver<ArraySegment<TElement>>
    {
        public override void Serialize(NetworkWriter writer, ArraySegment<TElement> instance)
        {
            Helper.Length.Write(writer, instance.Count);

            for (int i = 0; i < instance.Count; i++)
                writer.Write(instance[i]);
        }
        public override ArraySegment<TElement> Deserialize(NetworkReader reader)
        {
            Helper.Length.Read(reader, out var length);

            var array = new TElement[length];

            for (int i = 0; i < length; i++)
                array[i] = reader.Read<TElement>();

            return new ArraySegment<TElement>(array);
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
            var value = instance as IEnumerable;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            //TODO find a way to get the count to optimize this surrogate creation
            var list = Helper.List.Instantiate(argument, 0);

            foreach (var item in value) list.Add(item);

            Helper.Length.Write(writer, list.Count);

            for (int i = 0; i < list.Count; i++) writer.Write(list[i], argument);
        }
        public override object Deserialize(NetworkReader reader, Type type)
        {
            Helper.Length.Read(reader, out var count);

            Helper.GenericArguments.Retrieve(type, out Type argument);

            var array = Array.CreateInstance(argument, count) as IList;

            for (int i = 0; i < count; i++)
                array[i] = reader.Read(argument);

            var segment = Activator.CreateInstance(type, array);

            return segment;
        }
    }
    #endregion

    #region List
    [Preserve]
    public sealed class ListNetworkSerializationResolver<TElement> : NetworkSerializationDynamicResolver<List<TElement>>
    {
        public override void Serialize(NetworkWriter writer, List<TElement> instance)
        {
            if (Helper.Length.Collection.WriteGeneric(writer, instance) == false) return;

            for (int i = 0; i < instance.Count; i++) writer.Write(instance[i]);
        }
        public override List<TElement> Deserialize(NetworkReader reader)
        {
            if (Helper.Length.Collection.Read(reader, out var count) == false) return null;

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
            var list = instance as IList;

            if (Helper.Length.Collection.WriteExplicit(writer, list) == false) return;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            for (int i = 0; i < list.Count; i++) writer.Write(list[i], argument);
        }
        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (Helper.Length.Collection.Read(reader, out var count) == false) return null;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            var list = Helper.List.Instantiate(argument, count);

            for (int i = 0; i < count; i++)
            {
                var element = reader.Read(argument);

                list.Add(element);
            }

            return list;
        }
    }
    #endregion

    #region Hashset
    [Preserve]
    public sealed class HashSetNetworkSerializationResolver<TElement> : NetworkSerializationDynamicResolver<HashSet<TElement>>
    {
        public override void Serialize(NetworkWriter writer, HashSet<TElement> instance)
        {
            if (Helper.Length.Collection.WriteGeneric(writer, instance) == false) return;

            foreach (var item in instance) writer.Write(item);
        }
        public override HashSet<TElement> Deserialize(NetworkReader reader)
        {
            if (Helper.Length.Collection.Read(reader, out var length) == false) return null;

            var set = new HashSet<TElement>();

            for (int i = 0; i < length; i++)
            {
                var element = reader.Read<TElement>();

                set.Add(element);
            }

            return set;
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
            if (instance == null)
            {
                Helper.Length.Collection.WriteNull(writer);
                return;
            }

            var value = instance as IEnumerable;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            //TODO find a way to get the count to optimize this surrogate creation
            var list = Helper.List.Instantiate(argument, 0);

            foreach (var item in value) list.Add(item);

            Helper.Length.Collection.WriteValue(writer, list.Count);

            for (int i = 0; i < list.Count; i++) writer.Write(list[i], argument);
        }
        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (Helper.Length.Collection.Read(reader, out var length) == false) return null;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            var array = Array.CreateInstance(argument, length) as IList;

            for (int i = 0; i < length; i++)
                array[i] = reader.Read(argument);

            return Activator.CreateInstance(type, array);
        }
    }
    #endregion

    #region Queue
    [Preserve]
    public sealed class QueueNetworkSerializationResolver<TElement> : NetworkSerializationDynamicResolver<Queue<TElement>>
    {
        public override void Serialize(NetworkWriter writer, Queue<TElement> instance)
        {
            if (Helper.Length.Collection.WriteExplicit(writer, instance) == false) return;

            foreach (var item in instance) writer.Write(item);
        }
        public override Queue<TElement> Deserialize(NetworkReader reader)
        {
            if (Helper.Length.Collection.Read(reader, out var length) == false) return null;

            var queue = new Queue<TElement>(length);

            for (int i = 0; i < length; i++)
            {
                var element = reader.Read<TElement>();

                queue.Enqueue(element);
            }

            return queue;
        }
    }

    [Preserve]
    public sealed class QueueNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static bool IsValid(Type target)
        {
            if (target.IsGenericType == false) return false;

            return target.GetGenericTypeDefinition() == typeof(Queue<>);
        }
        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            if (instance == null)
            {
                Helper.Length.Collection.WriteNull(writer);
                return;
            }

            var collection = instance as ICollection;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            Helper.Length.Collection.WriteValue(writer, collection.Count);

            foreach (var item in collection) writer.Write(item, argument);
        }
        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (Helper.Length.Collection.Read(reader, out var length) == false) return null;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            var array = Array.CreateInstance(argument, length) as IList;

            for (int i = 0; i < length; i++)
                array[i] = reader.Read(argument);

            return Activator.CreateInstance(type, array);
        }
    }
    #endregion

    #region Stack
    [Preserve]
    public sealed class StackNetworkSerializationResolver<TElement> : NetworkSerializationDynamicResolver<Stack<TElement>>
    {
        public override void Serialize(NetworkWriter writer, Stack<TElement> instance)
        {
            if (Helper.Length.Collection.WriteExplicit(writer, instance) == false) return;

            foreach (var item in instance) writer.Write(item);
        }
        public override Stack<TElement> Deserialize(NetworkReader reader)
        {
            if (Helper.Length.Collection.Read(reader, out var length) == false) return null;

            var array = new TElement[length];

            for (int i = 0; i < length; i++)
                array[length - 1 - i] = reader.Read<TElement>();

            var stack = new Stack<TElement>(array);

            return stack;
        }
    }

    [Preserve]
    public sealed class StackNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static bool IsValid(Type target)
        {
            if (target.IsGenericType == false) return false;

            return target.GetGenericTypeDefinition() == typeof(Stack<>);
        }
        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            if (instance == null)
            {
                Helper.Length.Collection.WriteNull(writer);
                return;
            }

            var collection = instance as ICollection;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            Helper.Length.Collection.WriteValue(writer, collection.Count);

            foreach (var item in collection) writer.Write(item, argument);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (Helper.Length.Collection.Read(reader, out var length) == false) return null;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            var array = Array.CreateInstance(argument, length) as IList;

            for (int i = 0; i < length; i++)
                array[i] = reader.Read(argument);

            Array.Reverse(array as Array);

            return Activator.CreateInstance(type, array);
        }
    }
    #endregion

    #region Dictionary
    [Preserve]
    public sealed class DictionaryNetworkSerializationResolver<TKey, TValue> : NetworkSerializationDynamicResolver<Dictionary<TKey, TValue>>
    {
        public override void Serialize(NetworkWriter writer, Dictionary<TKey, TValue> instance)
        {
            if (Helper.Length.Collection.WriteGeneric(writer, instance) == false) return;

            foreach (var pair in instance)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }
        public override Dictionary<TKey, TValue> Deserialize(NetworkReader reader)
        {
            if (Helper.Length.Collection.Read(reader, out var count) == false) return null;

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
            var dictionary = instance as IDictionary;

            if (Helper.Length.Collection.WriteExplicit(writer, dictionary) == false) return;

            Helper.GenericArguments.Retrieve(type, out var keyType, out var valueType);

            foreach (DictionaryEntry entry in dictionary)
            {
                writer.Write(entry.Key, keyType);
                writer.Write(entry.Value, valueType);
            }
        }
        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (Helper.Length.Collection.Read(reader, out var count) == false) return null;

            var dictionary = Activator.CreateInstance(type, count) as IDictionary;

            Helper.GenericArguments.Retrieve(type, out var keyType, out var valueType);

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

    #region Tuple
    [Preserve]
    public sealed class TupleNetworkSerializationResolver<T1, T2> : NetworkSerializationDynamicResolver<(T1, T2)>
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
    public sealed class TupleNetworkSerializationResolver<T1, T2, T3> : NetworkSerializationDynamicResolver<(T1, T2, T3)>
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
    public sealed class TupleNetworkSerializationResolver<T1, T2, T3, T4> : NetworkSerializationDynamicResolver<(T1, T2, T3, T4)>
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
    public sealed class TupleNetworkSerializationResolver<T1, T2, T3, T4, T5> : NetworkSerializationDynamicResolver<(T1, T2, T3, T4, T5)>
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
    public sealed class TupleNetworkSerializationResolver<T1, T2, T3, T4, T5, T6> : NetworkSerializationDynamicResolver<(T1, T2, T3, T4, T5, T6)>
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
    public sealed class TupleNetworkSerializationResolver<T1, T2, T3, T4, T5, T6, T7> : NetworkSerializationDynamicResolver<(T1, T2, T3, T4, T5, T6, T7)>
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
    public sealed class TupleNetworkSerializationResolver<T1, T2, T3, T4, T5, T6, T7, T8> : NetworkSerializationDynamicResolver<(T1, T2, T3, T4, T5, T6, T7, T8)>
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

    [Preserve]
    public sealed class TupleNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static bool IsValid(Type target) => typeof(ITuple).IsAssignableFrom(target);
        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            if (WriteNull(writer, instance, type)) return;

            var tuple = instance as ITuple;

            Helper.GenericArguments.Retrieve(type, out Type[] arguments);

            for (int i = 0; i < tuple.Length; i++)
                writer.Write(tuple[i], arguments[i]);
        }
        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (ReadNull(reader, type)) return null;

            Helper.GenericArguments.Retrieve(type, out Type[] arguments);

            var items = new object[arguments.Length];

            for (int i = 0; i < arguments.Length; i++)
                items[i] = reader.Read(arguments[i]);

            var value = Activator.CreateInstance(type, items);

            return value;
        }
    }
    #endregion

    #region Network Serializable
    [Preserve]
    public sealed class INetworkSerializableResolver<T> : NetworkSerializationDynamicResolver<T>
        where T : INetworkSerializable, new()
    {
        bool nullable;

        public override void Serialize(NetworkWriter writer, T instance)
        {
            if (nullable) if (WriteNull(writer, instance)) return;

            var context = NetworkSerializationContext.Serialize(writer);

            instance.Select(ref context);
        }
        public override T Deserialize(NetworkReader reader)
        {
            if (nullable) if (ReadNull(reader)) return default;

            var value = new T();

            var context = NetworkSerializationContext.Deserialize(reader);

            value.Select(ref context);

            return value;
        }

        public INetworkSerializableResolver()
        {
            nullable = Helper.Nullable.Generic<T>.Is;
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

            var context = NetworkSerializationContext.Serialize(writer);

            value.Select(ref context);
        }
        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (ReadNull(reader, type)) return null;

            var value = Activator.CreateInstance(type, true) as INetworkSerializable;

            var context = NetworkSerializationContext.Deserialize(reader);

            value.Select(ref context);

            return value;
        }
    }

    [Preserve]
    public readonly ref struct NetworkSerializationContext
    {
        public NetworkWriter Writer { get; }
        public NetworkReader Reader { get; }

        public NetworkSerializationOperation Operation { get; }

        public bool IsSerializing => Operation == NetworkSerializationOperation.Serialization;
        public bool IsDeserializing => Operation == NetworkSerializationOperation.Deserialization;

        public void Select<T>(ref T value)
        {
            switch (Operation)
            {
                case NetworkSerializationOperation.Serialization:
                    Writer.Write(value);
                    break;

                case NetworkSerializationOperation.Deserialization:
                    Reader.Read(out value);
                    break;
            }
        }

        NetworkSerializationContext(NetworkWriter writer, NetworkReader reader, NetworkSerializationOperation operation)
        {
            this.Writer = writer;
            this.Reader = reader;

            this.Operation = operation;
        }

        public static NetworkSerializationContext Serialize(NetworkWriter writer)
        {
            return new NetworkSerializationContext(writer, default, NetworkSerializationOperation.Serialization);
        }
        public static NetworkSerializationContext Deserialize(NetworkReader reader)
        {
            return new NetworkSerializationContext(default, reader, NetworkSerializationOperation.Deserialization);
        }
    }

    public enum NetworkSerializationOperation
    {
        Serialization, Deserialization
    }
    #endregion

    #region Manual Network Serializable
    [Preserve]
    public sealed class IManualNetworkSerializableResolver<T> : NetworkSerializationDynamicResolver<T>
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
            nullable = Helper.Nullable.Generic<T>.Is;
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
    #endregion

    #region Enum
    [Preserve]
    public sealed class EnumNetworkSerializationResolver<TType, TBacking> : NetworkSerializationDynamicResolver<TType>
        where TType : struct, Enum
    {
        Dictionary<TBacking, TType> values;
        Dictionary<TType, TBacking> backings;

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
            values = new Dictionary<TBacking, TType>();
            backings = new Dictionary<TType, TBacking>();

            foreach (TType instance in Enum.GetValues(typeof(TType)))
            {
                var backing = ChangeType<TBacking>(instance);

                Register(backing, instance);
            }
        }
    }

    [Preserve]
    public sealed class EnumNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static bool IsValid(Type target) => target.IsEnum;
        public override bool CanResolve(Type target) => IsValid(target);

        public override void Serialize(NetworkWriter writer, object instance, Type type)
        {
            var value = Helper.Enum.Value.Retrieve(instance);

            writer.Write(value);
        }
        public override object Deserialize(NetworkReader reader, Type type)
        {
            var backing = Helper.Enum.UnderlyingType.Retrieve(type);

            var value = reader.Read(backing);

            var result = Enum.ToObject(type, value);

            return result;
        }
    }
    #endregion

    #region Nullable
    [Preserve]
    public sealed class NullableNetworkSerializationResolver<TData> : NetworkSerializationDynamicResolver<TData?>
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

            Helper.GenericArguments.Retrieve(type, out Type underlying);

            var value = reader.Read(underlying);

            return Activator.CreateInstance(type, value);
        }
    }
    #endregion

    #region Byte Array & Byte Segment
    [Preserve]
    public sealed class ByteArrayNetworkSerializationResolver : NetworkSerializationExplicitResolver<byte[]>
    {
        public override void Serialize(NetworkWriter writer, byte[] instance)
        {
            if (Helper.Length.Collection.WriteGeneric(writer, instance) == false) return;

            writer.Insert(instance);
        }
        public override byte[] Deserialize(NetworkReader reader)
        {
            if (Helper.Length.Collection.Read(reader, out var length) == false)
                return null;

            var value = reader.TakeArray(length);

            return value;
        }
    }

    [Preserve]
    public sealed class ByteArraySegmentSerilizationResolver : NetworkSerializationExplicitResolver<ArraySegment<byte>>
    {
        public override void Serialize(NetworkWriter writer, ArraySegment<byte> instance)
        {
            Helper.Length.Write(writer, instance.Count);

            writer.Insert(instance);
        }
        public override ArraySegment<byte> Deserialize(NetworkReader reader)
        {
            Helper.Length.Read(reader, out var length);

            var array = reader.TakeArray(length);

            return new ArraySegment<byte>(array);
        }
    }
    #endregion

    #region Object Arary & Object List
    [Preserve]
    public sealed class ObjectArrayNetworkSerializationResolver : NetworkSerializationExplicitResolver<object[]>
    {
        public override void Serialize(NetworkWriter writer, object[] instance)
        {
            if (Helper.Length.Collection.WriteGeneric(writer, instance) == false) return;

            for (int i = 0; i < instance.Length; i++)
            {
                if (instance[i] is null)
                {
                    writer.Write(typeof(void));
                }
                else
                {
                    var type = instance[i].GetType();

                    writer.Write(type);
                    writer.Write(instance[i]);
                }
            }
        }
        public override object[] Deserialize(NetworkReader reader)
        {
            if (Helper.Length.Collection.Read(reader, out var length) == false) return null;

            var array = new object[length];

            for (int i = 0; i < length; i++)
            {
                var type = reader.Read<Type>();

                if (type == typeof(void))
                    array[i] = null;
                else
                    array[i] = reader.Read(type);
            }

            return array;
        }
    }

    [Preserve]
    public sealed class ObjectListNetworkSerialziationResolver : NetworkSerializationExplicitResolver<List<object>>
    {
        public override void Serialize(NetworkWriter writer, List<object> instance)
        {
            if (Helper.Length.Collection.WriteGeneric(writer, instance) == false) return;

            for (int i = 0; i < instance.Count; i++)
            {
                if (instance[i] is null)
                {
                    writer.Write(typeof(void));
                }
                else
                {
                    var type = instance[i].GetType();

                    writer.Write(type);
                    writer.Write(instance[i]);
                }
            }
        }
        public override List<object> Deserialize(NetworkReader reader)
        {
            if (Helper.Length.Collection.Read(reader, out var length) == false) return null;

            var list = new List<object>(length);

            for (int i = 0; i < length; i++)
            {
                var type = reader.Read<Type>();

                if (type == typeof(void))
                {
                    list.Add(null);
                }
                else
                {
                    var item = reader.Read(type);
                    list.Add(item);
                }
            }

            return list;
        }
    }
    #endregion
}