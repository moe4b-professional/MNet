using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
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

                    result = type.IsValueType == false;

                    Dictionary.Add(type, result);

                    return result;
                }
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