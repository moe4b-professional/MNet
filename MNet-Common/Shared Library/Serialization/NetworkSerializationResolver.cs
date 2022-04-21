using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Helper = MNet.NetworkSerializationHelper;

namespace MNet
{
    [Preserve]
    public class NetworkSerializationResolver
    {
        public virtual IEnumerable<Type> Children { get; } = null;

        static readonly object SyncLock = new object();

        public static ExplicitNetworkSerializationResolver<T> Retrieve<T>()
        {
            lock (SyncLock)
            {
                if (ExplicitNetworkSerializationResolver<T>.Instance != null)
                    return ExplicitNetworkSerializationResolver<T>.Instance;

                var type = typeof(T);
                if (DynamicNetworkSerialization.Resolve(type, out var resolver))
                {
#if MNet_Generated_AOT_Code
                    Log.Warning($"Dynamic Resolver ({resolver.GetType()}) for ({type}) Created When AOT Code is Present, Please Re-Generate AOT Code");
#endif
                }

                return ExplicitNetworkSerializationResolver<T>.Instance;
            }
        }

        static NetworkSerializationResolver()
        {
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
                }
            }
        }
    }

    #region Resolvers
    [Preserve]
    public abstract class ExplicitNetworkSerializationResolver<T> : NetworkSerializationResolver
    {
        internal static ExplicitNetworkSerializationResolver<T> Instance;

        public abstract void Serialize(NetworkWriter writer, T instance);
        public abstract T Deserialize(NetworkReader reader);

        public ExplicitNetworkSerializationResolver()
        {
            Instance = this;
        }

        #region Null
        protected static bool WriteNull(NetworkWriter writer, T value) => WriteNull(writer, value == null);
        protected static bool WriteNull(NetworkWriter writer, bool value)
        {
            if (value)
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

        protected static bool ReadNull(NetworkReader reader)
        {
            return reader.TakeByte() == 1 ? true : false;
        }
        #endregion
    }

    [Preserve]
    public abstract class DynamicNetworkSerializationResolver<T> : ExplicitNetworkSerializationResolver<T>
    {

    }
    #endregion

    [Preserve]
    public static class DynamicNetworkSerialization
    {
        public static List<ResolveDelegate> Resolvers { get; private set; }
        public delegate bool ResolveDelegate(Type type, ref NetworkSerializationResolver resolver);

        internal static bool Resolve(Type type, out NetworkSerializationResolver resolver)
        {
            resolver = default;

            for (int i = 0; i < Resolvers.Count; i++)
                if (Resolvers[i].Invoke(type, ref resolver))
                    return true;

            return false;
        }

        public static void Register(ResolveDelegate method) => Resolvers.Add(method);

        static DynamicNetworkSerialization()
        {
            Resolvers = new List<ResolveDelegate>();

            Register(ResolveINetworkSerializable);
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

            Register(ResolveBlittable);

            Register(ResolveFixedString);
        }

        #region Default Resolvers
        static bool ResolveINetworkSerializable(Type type, ref NetworkSerializationResolver resolver)
        {
            if (Helper.TypeChecks.IsINetworkSerializable(type) == false)
                return false;

            resolver = ConstructResolver(typeof(INetworkSerializableResolver<>), type);
            return true;
        }

        static bool ResolveIManualNetworkSerilizable(Type type, ref NetworkSerializationResolver resolver)
        {
            if (Helper.TypeChecks.IsIManualNetworkSerializable(type) == false)
                return false;

            resolver = ConstructResolver(typeof(IManualNetworkSerializableResolver<>), type);
            return true;
        }

        static bool ResolveEnum(Type type, ref NetworkSerializationResolver resolver)
        {
            if (Helper.TypeChecks.IsEnum(type) == false)
                return false;

            resolver = ConstructResolver(typeof(EnumNetworkSerializationResolver<>), type);
            return true;
        }

        static bool ResolveNullable(Type type, ref NetworkSerializationResolver resolver)
        {
            if (Helper.TypeChecks.IsNullable(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(NullableNetworkSerializationResolver<>), argument);
            return true;
        }

        static bool ResolveTuple(Type type, ref NetworkSerializationResolver resolver)
        {
            if (type.IsValueType == false)
                return false;

            if (Helper.TypeChecks.IsTuple(type) == false)
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
            if (Helper.TypeChecks.IsArray(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(ArrayNetworkSerializationResolver<>), argument);
            return true;
        }

        static bool ResolveArraySegment(Type type, ref NetworkSerializationResolver resolver)
        {
            if (Helper.TypeChecks.IsArraySegment(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(ArraySegmentNetworkSerializationResolver<>), argument);
            return true;
        }

        static bool ResolveList(Type type, ref NetworkSerializationResolver resolver)
        {
            if (Helper.TypeChecks.IsList(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(ListNetworkSerializationResolver<>), argument);
            return true;
        }

        static bool ResolveHashSet(Type type, ref NetworkSerializationResolver resolver)
        {
            if (Helper.TypeChecks.IsHashset(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(HashSetNetworkSerializationResolver<>), argument);
            return true;
        }

        static bool ResolveQueue(Type type, ref NetworkSerializationResolver resolver)
        {
            if (Helper.TypeChecks.IsQueue(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(QueueNetworkSerializationResolver<>), argument);
            return true;
        }

        static bool ResolveStack(Type type, ref NetworkSerializationResolver resolver)
        {
            if (Helper.TypeChecks.IsStack(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type argument);

            resolver = ConstructResolver(typeof(StackNetworkSerializationResolver<>), argument);
            return true;
        }

        static bool ResolveDictionary(Type type, ref NetworkSerializationResolver resolver)
        {
            if (Helper.TypeChecks.IsDicitionary(type) == false)
                return false;

            Helper.GenericArguments.Retrieve(type, out Type key, out Type value);

            resolver = ConstructResolver(typeof(DictionaryNetworkSerializationResolver<,>), key, value);
            return true;
        }

        static bool ResolveBlittable(Type type, ref NetworkSerializationResolver resolver)
        {
            if (Helper.TypeChecks.IsBlittable(type) == false)
                return false;

            resolver = ConstructResolver(typeof(BlittableNetworkSerializationResolver<>), type);
            return true;
        }

        static bool ResolveFixedString(Type type, ref NetworkSerializationResolver resolver)
        {
            if (Helper.TypeChecks.IsFixedString(type) == false)
                return false;

            resolver = ConstructResolver(typeof(FixedStringNetworkSerializationResolver<>), type);
            return true;
        }
        #endregion

        //Utility

        public delegate void ConstructResolverDelegate(Type definition, Type[] arguments, NetworkSerializationResolver resolver);
        public static event ConstructResolverDelegate OnConstructResolver;

        public static NetworkSerializationResolver ConstructResolver(Type definition, params Type[] arguments)
        {
            if (typeof(NetworkSerializationResolver).IsAssignableFrom(definition) == false)
                throw new ArgumentException($"Cannot use Type ({definition}) to Create a Network Serialization Resolver");

            var target = definition.MakeGenericType(arguments);

            var resolver = Activator.CreateInstance(target) as NetworkSerializationResolver;

            OnConstructResolver?.Invoke(definition, arguments, resolver);

            return resolver;
        }
    }

    #region Primitive
    [Preserve]
    public sealed class ByteNetworkSerializationResolver : ExplicitNetworkSerializationResolver<byte>
    {
        public override void Serialize(NetworkWriter writer, byte instance)
        {
            Helper.Blittable.Serialize(writer, instance);
        }
        public override byte Deserialize(NetworkReader reader)
        {
            return Helper.Blittable.Deserialize<byte>(reader);
        }
    }
    [Preserve]
    public sealed class SByteNetworkSerializationResolver : ExplicitNetworkSerializationResolver<sbyte>
    {
        public override void Serialize(NetworkWriter writer, sbyte instance)
        {
            Helper.Blittable.Serialize(writer, instance);
        }
        public override sbyte Deserialize(NetworkReader reader)
        {
            return Helper.Blittable.Deserialize<sbyte>(reader);
        }
    }

    [Preserve]
    public sealed class BoolNetworkSerializationResolver : ExplicitNetworkSerializationResolver<bool>
    {
        public override void Serialize(NetworkWriter writer, bool instance)
        {
            Helper.Blittable.Serialize(writer, instance ? (byte)1 : (byte)0);
        }
        public override bool Deserialize(NetworkReader reader)
        {
            return Helper.Blittable.Deserialize<byte>(reader) == 0 ? false : true;
        }
    }

    [Preserve]
    public sealed class ShortNetworkSerializationResolver : ExplicitNetworkSerializationResolver<short>
    {
        public override void Serialize(NetworkWriter writer, short instance)
        {
            Helper.Blittable.Serialize(writer, instance);
        }
        public override short Deserialize(NetworkReader reader)
        {
            return Helper.Blittable.Deserialize<short>(reader);
        }
    }
    [Preserve]
    public sealed class UShortNetworkSerializationResolver : ExplicitNetworkSerializationResolver<ushort>
    {
        public override void Serialize(NetworkWriter writer, ushort instance)
        {
            Helper.Blittable.Serialize(writer, instance);
        }
        public override ushort Deserialize(NetworkReader reader)
        {
            return Helper.Blittable.Deserialize<ushort>(reader);
        }
    }

    [Preserve]
    public sealed class IntNetworkSerializationResolver : ExplicitNetworkSerializationResolver<int>
    {
        public override void Serialize(NetworkWriter writer, int instance)
        {
            Helper.Blittable.Serialize(writer, instance);
        }
        public override int Deserialize(NetworkReader reader)
        {
            return Helper.Blittable.Deserialize<int>(reader);
        }
    }
    [Preserve]
    public sealed class UIntNetworkSerializationResolver : ExplicitNetworkSerializationResolver<uint>
    {
        public override void Serialize(NetworkWriter writer, uint instance)
        {
            Helper.Blittable.Serialize(writer, instance);
        }
        public override uint Deserialize(NetworkReader reader)
        {
            return Helper.Blittable.Deserialize<uint>(reader);
        }
    }

    [Preserve]
    public sealed class LongNetworkSerializationResolver : ExplicitNetworkSerializationResolver<long>
    {
        public override void Serialize(NetworkWriter writer, long instance)
        {
            Helper.Blittable.Serialize(writer, instance);
        }
        public override long Deserialize(NetworkReader reader)
        {
            return Helper.Blittable.Deserialize<long>(reader);
        }
    }
    [Preserve]
    public sealed class ULongNetworkSerializationResolver : ExplicitNetworkSerializationResolver<ulong>
    {
        public override void Serialize(NetworkWriter writer, ulong instance)
        {
            Helper.Blittable.Serialize(writer, instance);
        }
        public override ulong Deserialize(NetworkReader reader)
        {
            return Helper.Blittable.Deserialize<ulong>(reader);
        }
    }

    [Preserve]
    public sealed class FloatNetworkSerializationResolver : ExplicitNetworkSerializationResolver<float>
    {
        public override void Serialize(NetworkWriter writer, float instance)
        {
            Helper.Blittable.Serialize(writer, instance);
        }
        public override float Deserialize(NetworkReader reader)
        {
            return Helper.Blittable.Deserialize<float>(reader);
        }
    }

    [Preserve]
    public sealed class DoubleNetworkSerializationResolver : ExplicitNetworkSerializationResolver<double>
    {
        public override void Serialize(NetworkWriter writer, double instance)
        {
            Helper.Blittable.Serialize(writer, instance);
        }
        public override double Deserialize(NetworkReader reader)
        {
            return Helper.Blittable.Deserialize<double>(reader);
        }
    }

    [Preserve]
    public sealed class StringNetworkSerializationResolver : ExplicitNetworkSerializationResolver<string>
    {
        public const int MaxStackAllocationSize = 512;

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
    public class GuidNetworkSerializationResolver : ExplicitNetworkSerializationResolver<Guid>
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
    public class DateTimeNetworkSerializationResolver : ExplicitNetworkSerializationResolver<DateTime>
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
    public class TimeSpanNetworkSerializationResolver : ExplicitNetworkSerializationResolver<TimeSpan>
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
    public class IPAddressNetworkSerializationResolver : ExplicitNetworkSerializationResolver<IPAddress>
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
    public class TypeNetworkSerializationResolver : ExplicitNetworkSerializationResolver<Type>
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
    public sealed class ArrayNetworkSerializationResolver<TElement> : DynamicNetworkSerializationResolver<TElement[]>
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(TElement);
            }
        }

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
    #endregion

    #region Array Segment
    [Preserve]
    public sealed class ArraySegmentNetworkSerializationResolver<TElement> : DynamicNetworkSerializationResolver<ArraySegment<TElement>>
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(TElement);
            }
        }

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
    #endregion

    #region List
    [Preserve]
    public sealed class ListNetworkSerializationResolver<TElement> : DynamicNetworkSerializationResolver<List<TElement>>
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(TElement);
            }
        }

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
    #endregion

    #region Hashset
    [Preserve]
    public sealed class HashSetNetworkSerializationResolver<TElement> : DynamicNetworkSerializationResolver<HashSet<TElement>>
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(TElement);
            }
        }

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
    #endregion

    #region Queue
    [Preserve]
    public sealed class QueueNetworkSerializationResolver<TElement> : DynamicNetworkSerializationResolver<Queue<TElement>>
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(TElement);
            }
        }

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
    #endregion

    #region Stack
    [Preserve]
    public sealed class StackNetworkSerializationResolver<TElement> : DynamicNetworkSerializationResolver<Stack<TElement>>
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(TElement);
            }
        }

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
    #endregion

    #region Dictionary
    [Preserve]
    public sealed class DictionaryNetworkSerializationResolver<TKey, TValue> : DynamicNetworkSerializationResolver<Dictionary<TKey, TValue>>
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(TKey);
                yield return typeof(TValue);
            }
        }

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
    #endregion

    #region Tuple
    [Preserve]
    public sealed class TupleNetworkSerializationResolver<T1, T2> : DynamicNetworkSerializationResolver<(T1, T2)>
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(T1);
                yield return typeof(T2);
            }
        }

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
    public sealed class TupleNetworkSerializationResolver<T1, T2, T3> : DynamicNetworkSerializationResolver<(T1, T2, T3)>
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(T1);
                yield return typeof(T2);
                yield return typeof(T3);
            }
        }

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
    public sealed class TupleNetworkSerializationResolver<T1, T2, T3, T4> : DynamicNetworkSerializationResolver<(T1, T2, T3, T4)>
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(T1);
                yield return typeof(T2);
                yield return typeof(T3);
                yield return typeof(T4);
            }
        }

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
    public sealed class TupleNetworkSerializationResolver<T1, T2, T3, T4, T5> : DynamicNetworkSerializationResolver<(T1, T2, T3, T4, T5)>
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(T1);
                yield return typeof(T2);
                yield return typeof(T3);
                yield return typeof(T4);
                yield return typeof(T5);
            }
        }

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
    public sealed class TupleNetworkSerializationResolver<T1, T2, T3, T4, T5, T6> : DynamicNetworkSerializationResolver<(T1, T2, T3, T4, T5, T6)>
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(T1);
                yield return typeof(T2);
                yield return typeof(T3);
                yield return typeof(T4);
                yield return typeof(T5);
                yield return typeof(T6);
            }
        }

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
    public sealed class TupleNetworkSerializationResolver<T1, T2, T3, T4, T5, T6, T7> : DynamicNetworkSerializationResolver<(T1, T2, T3, T4, T5, T6, T7)>
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(T1);
                yield return typeof(T2);
                yield return typeof(T3);
                yield return typeof(T4);
                yield return typeof(T5);
                yield return typeof(T6);
                yield return typeof(T7);
            }
        }

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
    public sealed class TupleNetworkSerializationResolver<T1, T2, T3, T4, T5, T6, T7, T8> : DynamicNetworkSerializationResolver<(T1, T2, T3, T4, T5, T6, T7, T8)>
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(T1);
                yield return typeof(T2);
                yield return typeof(T3);
                yield return typeof(T4);
                yield return typeof(T5);
                yield return typeof(T6);
                yield return typeof(T7);
                yield return typeof(T8);
            }
        }

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

    #region Network Serializable
    [Preserve]
    public sealed class INetworkSerializableResolver<T> : DynamicNetworkSerializationResolver<T>
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
            nullable = Helper.Nullable.Evaluate<T>();
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

        public void Select<[NetworkSerializationGenerator] T>(ref T value)
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
    public sealed class IManualNetworkSerializableResolver<T> : DynamicNetworkSerializationResolver<T>
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
            nullable = Helper.Nullable.Evaluate<T>();
        }
    }
    #endregion

    #region Enum
    [Preserve]
    public sealed unsafe class EnumNetworkSerializationResolver<TType> : DynamicNetworkSerializationResolver<TType>
        where TType : unmanaged, Enum
    {
        public override void Serialize(NetworkWriter writer, TType instance)
        {
            Helper.Blittable.Serialize(writer, instance);
        }
        public override TType Deserialize(NetworkReader reader)
        {
            return Helper.Blittable.Deserialize<TType>(reader);
        }
    }
    #endregion

    #region Nullable
    [Preserve]
    public sealed class NullableNetworkSerializationResolver<TData> : DynamicNetworkSerializationResolver<TData?>
        where TData : struct
    {
        public override IEnumerable<Type> Children
        {
            get
            {
                yield return typeof(TData);
            }
        }

        public override void Serialize(NetworkWriter writer, TData? instance)
        {
            if (WriteNull(writer, instance.HasValue == false)) return;

            writer.Write(instance.Value);
        }
        public override TData? Deserialize(NetworkReader reader)
        {
            if (ReadNull(reader))
                return default;

            return reader.Read<TData>();
        }
    }
    #endregion

    #region Byte Array & Byte Segment
    [Preserve]
    public sealed class ByteArrayNetworkSerializationResolver : ExplicitNetworkSerializationResolver<byte[]>
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
    public sealed class ByteArraySegmentSerilizationResolver : ExplicitNetworkSerializationResolver<ArraySegment<byte>>
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

    #region Blittable
    public unsafe class BlittableNetworkSerializationResolver<T> : DynamicNetworkSerializationResolver<T>
        where T : unmanaged
    {
        int Size;

        public override void Serialize(NetworkWriter writer, T instance)
        {
            writer.Fit(Size);

            fixed (byte* destination = &writer.Data[writer.Position])
            {
#if UNITY_ANDROID
                var source = &instance;
                Buffer.MemoryCopy(source, destination, writer.Remaining, Size);
#else
                ref var reference = ref Unsafe.AsRef<T>(destination);
                reference = instance;
#endif
            }

            writer.Position += Size;
        }
        public override T Deserialize(NetworkReader reader)
        {
            var value = new T();

            fixed (byte* source = &reader.Data[reader.Position])
            {
#if UNITY_ANDROID
                var destination = &value;
                Buffer.MemoryCopy(source, destination, reader.Remaining, Size);
#else
                value = Unsafe.AsRef<T>(source);
#endif
            }

            reader.Position += Size;

            return value;
        }

        public BlittableNetworkSerializationResolver()
        {
            Size = sizeof(T);
        }
    }

    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class NetworkBlittableAttribute : Attribute { }
    #endregion

    #region Fixed String
    [Preserve]
    public sealed class FixedStringNetworkSerializationResolver<T> : DynamicNetworkSerializationResolver<T>
        where T : IFixedString, new()
    {
        public const int MaxSize = FixedString.MaxSize;

        public override void Serialize(NetworkWriter writer, T instance)
        {
            Helper.Length.Write(writer, instance.Length);

            if (instance.Length > MaxSize)
                throw new InvalidOperationException($"Cannot Serialize Fixed String With Size of {instance.Length}");

            if (instance.Length == 0)
                return;

            var characters = instance.ToSpan();

            var size = Encoding.UTF8.GetByteCount(characters);

            Span<byte> buffer = stackalloc byte[size];

            Encoding.UTF8.GetBytes(characters, buffer);

            writer.Insert(buffer);
        }
        public override T Deserialize(NetworkReader reader)
        {
            var length = Helper.Length.Read(reader);

            if (length > MaxSize)
                throw new InvalidOperationException($"Cannot Deserialize Fixed String With Size of {length}");

            if (length == 0)
                return default;

            var value = new T();
            value.Length = length;

            var characters = value.ToSpan();
            var buffer = reader.TakeSpan(length);

            var read = Encoding.UTF8.GetChars(buffer, characters);

            if (read != length)
                throw new Exception("Mismathed Read Length When Deserializing Fixed String" + read);

            return value;
        }
    }
    #endregion
}