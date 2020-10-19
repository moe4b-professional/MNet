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
    }
}