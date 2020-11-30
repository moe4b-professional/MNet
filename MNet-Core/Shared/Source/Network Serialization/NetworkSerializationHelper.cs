﻿using System;

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
            public static ConcurrentDictionary<Type, bool> Dictionary { get; private set; }

            public static bool Check(Type type)
            {
                if (Dictionary.TryGetValue(type, out var value)) return value;

                value = Evaluate(type);

                Dictionary.TryAdd(type, value);

                return value;
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

            static Nullable()
            {
                Dictionary = new ConcurrentDictionary<Type, bool>();
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

            public static void Read(NetworkReader reader, out ushort length) => reader.Read(out length);
        }

        public static class GenericArguments
        {
            public static ConcurrentDictionary<Type, Type[]> Dictionary { get; private set; }

            public static void Retrieve(Type type, out Type element)
            {
                if (Dictionary.TryGetValue(type, out var elements))
                {
                    element = elements[0];

                    return;
                }

                if (type.IsArray)
                {
                    element = type.GetElementType();
                    elements = new Type[] { element };

                    Dictionary.TryAdd(type, elements);

                    return;
                }

                if (type.IsGenericType)
                {
                    elements = type.GetGenericArguments();
                    element = elements[0];

                    Dictionary.TryAdd(type, elements);

                    return;
                }

                element = null;
            }

            public static void Retrieve(Type type, out Type element1, out Type element2)
            {
                if (Dictionary.TryGetValue(type, out var elements))
                {
                    element1 = elements[0];
                    element2 = elements[1];

                    return;
                }

                if (type.IsGenericType)
                {
                    elements = type.GetGenericArguments();

                    if (elements.Length >= 2)
                    {
                        element1 = elements[0];
                        element2 = elements[1];

                        Dictionary.TryAdd(type, elements);

                        return;
                    }
                }

                element1 = null;
                element2 = null;
            }

            public static void Retrieve(Type type, out Type[] elements)
            {
                if (Dictionary.TryGetValue(type, out elements)) return;

                if (type.IsGenericType)
                {
                    elements = type.GetGenericArguments();

                    Dictionary.TryAdd(type, elements);

                    return;
                }

                elements = null;
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
    }
}