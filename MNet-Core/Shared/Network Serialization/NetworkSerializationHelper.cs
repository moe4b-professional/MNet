using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    static class NetworkSerializationHelper
    {
        public static class Nullable
        {
            public static Dictionary<Type, bool> Dictionary { get; private set; }

            static object SyncLock = new object();

            public static bool Check(Type type)
            {
                lock (SyncLock)
                {
                    if (Dictionary.TryGetValue(type, out var result)) return result;

                    result = Evaluate(type);

                    Dictionary.Add(type, result);

                    return result;
                }
            }

            static bool Evaluate(Type type)
            {
                if(type.IsValueType)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) return true;

                    return false;
                }

                return true;
            }

            static Nullable()
            {
                Dictionary = new Dictionary<Type, bool>();
            }
        }

        public static class Length
        {
            public static void Write(int source, NetworkWriter writer)
            {
                if (source > ushort.MaxValue)
                    throw new Exception($"Cannot Serialize {source} as a ushort Code, It's Value is Above the Maximum Value of {ushort.MaxValue}");

                var length = (ushort)source;

                writer.Write(length);
            }

            public static void Read(out ushort length, NetworkReader reader) => reader.Read(out length);
        }

        public static class CollectionElement
        {
            public static Dictionary<Type, Type> Single { get; private set; }
            static void Register(Type type, Type element) => Single.Add(type, element);

            public static Dictionary<Type, (Type, Type)> Double { get; private set; }
            static void Register(Type type, Type element1, Type element2) => Double.Add(type, (element1, element2));

            public static void Retrieve(Type type, out Type element)
            {
                if (Single.TryGetValue(type, out element)) return;

                if (type.IsArray)
                {
                    element = type.GetElementType();
                    Register(type, element);
                    return;
                }

                if (type.IsGenericType)
                {
                    var arguments = type.GetGenericArguments();

                    element = arguments[0];

                    Register(type, element);

                    return;
                }
            }

            public static void Retrieve(Type type, out Type element1, out Type element2)
            {
                if(Double.TryGetValue(type, out var result))
                {
                    element1 = result.Item1;
                    element2 = result.Item2;
                    return;
                }

                if(type.IsGenericType)
                {
                    var arguments = type.GetGenericArguments();

                    if (arguments.Length >= 2)
                    {
                        element1 = arguments[0];
                        element2 = arguments[1];

                        Register(type, element1, element2);

                        return;
                    }
                }

                element1 = null;
                element2 = null;
            }

            static CollectionElement()
            {
                Single = new Dictionary<Type, Type>();
                Double = new Dictionary<Type, (Type, Type)>();
            }
        }
    }
}