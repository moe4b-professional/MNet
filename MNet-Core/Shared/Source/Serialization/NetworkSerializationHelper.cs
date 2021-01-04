using System;

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    public static class NetworkSerializationHelper
    {
        public static class Nullable
        {
            public static class Any
            {
                public static ConcurrentDictionary<Type, bool> Dictionary { get; private set; }

                public static bool Check(Type type)
                {
                    if (Dictionary.TryGetValue(type, out var value)) return value;

                    value = Evaluate(type);

                    Dictionary.TryAdd(type, value);

                    return value;
                }

                static Any()
                {
                    Dictionary = new ConcurrentDictionary<Type, bool>();
                }
            }

            public static class Generic<T>
            {
                static int value = -1;

                public static bool Is
                {
                    get
                    {
                        switch (value)
                        {
                            case -1:
                                return Set();
                            case 0:
                                return false;
                            case 1:
                                return true;
                        }

                        return true;
                    }
                }

                static bool Set()
                {
                    var result = Evaluate<T>();

                    value = result ? 1 : 0;

                    return result;
                }
            }

            static bool Evaluate<T>()
            {
                var type = typeof(T);

                return Evaluate(type);
            }
            static bool Evaluate(Type type)
            {
                if (type.IsValueType)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) return true;

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
        }

        public static class GenericArguments
        {
            public static ConcurrentDictionary<Type, Type[]> Dictionary { get; private set; }

            public static void Retrieve(Type type, out Type argument)
            {
                if (Dictionary.TryGetValue(type, out var elements))
                {
                    argument = elements[0];

                    return;
                }

                if (type.IsArray)
                {
                    argument = type.GetElementType();
                    elements = new Type[] { argument };

                    Dictionary.TryAdd(type, elements);

                    return;
                }

                if (type.IsGenericType)
                {
                    elements = type.GetGenericArguments();
                    argument = elements[0];

                    Dictionary.TryAdd(type, elements);

                    return;
                }

                argument = null;
            }

            public static void Retrieve(Type type, out Type argument1, out Type argument2)
            {
                if (Dictionary.TryGetValue(type, out var elements))
                {
                    argument1 = elements[0];
                    argument2 = elements[1];

                    return;
                }

                if (type.IsGenericType)
                {
                    elements = type.GetGenericArguments();

                    if (elements.Length >= 2)
                    {
                        argument1 = elements[0];
                        argument2 = elements[1];

                        Dictionary.TryAdd(type, elements);

                        return;
                    }
                }

                argument1 = null;
                argument2 = null;
            }

            public static void Retrieve(Type type, out Type[] arguments)
            {
                if (Dictionary.TryGetValue(type, out arguments)) return;

                if (type.IsGenericType)
                {
                    arguments = type.GetGenericArguments();

                    Dictionary.TryAdd(type, arguments);

                    return;
                }

                arguments = null;
            }

            static GenericArguments()
            {
                Dictionary = new ConcurrentDictionary<Type, Type[]>();
            }
        }

        public static class List
        {
            public static Type GenericDefinition => typeof(List<>);

            public static Type Construct(Type argument) => GenericDefinition.MakeGenericType(argument);

            public static IList Instantiate(Type argument, int size)
            {
                var type = Construct(argument);

                var instance = Activator.CreateInstance(type, size) as IList;

                return instance;
            }

            public static IList ReadFrom(NetworkReader reader, Type argument)
            {
                var type = Construct(argument);

                var list = reader.Read(type) as IList;

                return list;
            }
        }

        public static class Enum
        {
            public static class UnderlyingType
            {
                public static ConcurrentDictionary<Type, Type> Dictionary { get; private set; }

                public static Type Retrieve(Type type)
                {
                    if (Dictionary.TryGetValue(type, out var underlying)) return underlying;

                    underlying = System.Enum.GetUnderlyingType(type);

                    Dictionary.TryAdd(type, underlying);

                    return underlying;
                }
                public static Type Retrieve<T>()
                {
                    var type = typeof(T);

                    return Retrieve(type);
                }

                static UnderlyingType()
                {
                    Dictionary = new ConcurrentDictionary<Type, Type>();
                }
            }

            public static class Value
            {
                public static ConcurrentDictionary<object, object> Dictionary { get; private set; }

                public static object Retrieve(object element)
                {
                    if (Dictionary.TryGetValue(element, out var value)) return value;

                    var type = element.GetType();

                    var backing = UnderlyingType.Retrieve(type);

                    value = Convert.ChangeType(element, backing);

                    Dictionary.TryAdd(element, value);

                    return value;
                }

                static Value()
                {
                    Dictionary = new ConcurrentDictionary<object, object>();
                }
            }
        }
    }
}