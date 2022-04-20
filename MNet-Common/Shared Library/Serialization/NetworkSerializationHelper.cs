using System;

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    public static class NetworkSerializationHelper
    {
        public static class Nullable
        {
            public static bool Evaluate<T>()
            {
                var type = typeof(T);

                return Evaluate(type);
            }
            public static bool Evaluate(Type type)
            {
                if (type.IsValueType)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        return true;

                    return false;
                }

                return true;
            }
        }

        public static class Length
        {
            public static void Write(NetworkWriter writer, int value)
            {
                if (value > ushort.MaxValue)
                    throw new Exception($"Cannot Serialize {value} as a ushort Code, It's Value is Above the Maximum Value of {ushort.MaxValue}");

                var length = (ushort)value;

                writer.Write(length);
            }

            public static void Read(NetworkReader reader, out ushort length) => length = Read(reader);
            public static ushort Read(NetworkReader reader) => reader.Read<ushort>();

            public static class Collection
            {
                public static bool WriteExplicit(NetworkWriter writer, ICollection collection)
                {
                    if (collection == null)
                    {
                        WriteNull(writer);
                        return false;
                    }
                    else
                    {
                        WriteValue(writer, collection.Count);
                        return true;
                    }
                }

                public static bool WriteGeneric<T>(NetworkWriter writer, ICollection<T> collection)
                {
                    if (collection == null)
                    {
                        WriteNull(writer);
                        return false;
                    }
                    else
                    {
                        WriteValue(writer, collection.Count);
                        return true;
                    }
                }

                public static void WriteNull(NetworkWriter writer)
                {
                    writer.Write<ushort>(0);
                }
                public static void WriteValue(NetworkWriter writer, int value)
                {
                    writer.Write((ushort)(value + 1));
                }

                public static bool Read(NetworkReader reader, out ushort value)
                {
                    value = reader.Read<ushort>();

                    if (value == 0) return false;

                    value -= 1;
                    return true;
                }
            }
        }

        public static class GenericArguments
        {
            public static void Retrieve(Type type, out Type argument)
            {
                if (type.IsArray)
                {
                    argument = type.GetElementType();
                    return;
                }

                if (type.IsGenericType)
                {
                    var elements = type.GetGenericArguments();
                    argument = elements[0];

                    return;
                }

                argument = null;
            }

            public static void Retrieve(Type type, out Type argument1, out Type argument2)
            {
                if (type.IsGenericType)
                {
                    var elements = type.GetGenericArguments();

                    if (elements.Length >= 2)
                    {
                        argument1 = elements[0];
                        argument2 = elements[1];

                        return;
                    }
                }

                argument1 = null;
                argument2 = null;
            }

            public static void Retrieve(Type type, out Type[] arguments)
            {
                if (type.IsGenericType)
                {
                    arguments = type.GetGenericArguments();

                    return;
                }

                arguments = null;
            }
        }

        public static class TypeChecks
        {
            public static bool IsNullable(Type target)
            {
                if (target.IsGenericType == false) return false;

                return target.GetGenericTypeDefinition() == typeof(Nullable<>);
            }

            public static bool IsEnum(Type target)
            {
                return target.IsEnum;
            }

            public static bool IsIManualNetworkSerializable(Type target)
            {
                return typeof(IManualNetworkSerializable).IsAssignableFrom(target);
            }

            public static bool IsINetworkSerializable(Type target)
            {
                return typeof(INetworkSerializable).IsAssignableFrom(target);
            }

            public static bool IsTuple(Type target)
            {
                return typeof(ITuple).IsAssignableFrom(target);
            }

            public static bool IsDicitionary(Type target)
            {
                if (target.IsGenericType == false)
                    return false;

                return target.GetGenericTypeDefinition() == typeof(Dictionary<,>);
            }

            public static bool IsStack(Type target)
            {
                if (target.IsGenericType == false)
                    return false;

                return target.GetGenericTypeDefinition() == typeof(Stack<>);
            }

            public static bool IsQueue(Type target)
            {
                if (target.IsGenericType == false)
                    return false;

                return target.GetGenericTypeDefinition() == typeof(Queue<>);
            }

            public static bool IsHashset(Type target)
            {
                if (target.IsGenericType == false) return false;

                return target.GetGenericTypeDefinition() == typeof(HashSet<>);
            }

            public static bool IsList(Type target)
            {
                if (target.IsGenericType == false)
                    return false;

                return target.GetGenericTypeDefinition() == typeof(List<>);
            }

            public static bool IsArraySegment(Type target)
            {
                if (target.IsGenericType == false)
                    return false;

                return target.GetGenericTypeDefinition() == typeof(ArraySegment<>);
            }

            public static bool IsArray(Type target)
            {
                if (target.IsArray == false)
                    return false;

                if (target.GetArrayRank() != 1)
                    return false;

                return true;
            }

            public static bool IsBlittable(Type target)
            {
                var blittable = target.GetCustomAttribute<NetworkBlittableAttribute>();
                if (blittable is null)
                    return false;

                var layout = target.StructLayoutAttribute;
                if (layout is null)
                    throw new InvalidOperationException($"({target}) is Marked Blittable but Doesn't Have a Struct Layout Attribute");
                else if (layout.Value != LayoutKind.Sequential)
                    throw new InvalidOperationException($"({target}) is Marked Blittable but It's Struct Layout Attribute isn't Sequential");

                return true;
            }

            public static bool IsFixedString(Type target)
            {
                if (typeof(IFixedString).IsAssignableFrom(target) == false)
                    return false;

                return true;
            }
        }

        public static class Blittable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void Serialize<T>(NetworkWriter writer, T instance)
                where T : unmanaged
            {
                writer.Fit(sizeof(T));

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

                writer.Position += sizeof(T);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe T Deserialize<T>(NetworkReader reader)
                where T : unmanaged
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

                reader.Position += sizeof(T);

                return value;
            }
        }
    }
}