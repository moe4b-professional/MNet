using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using ProtoBuf;

namespace Game.Fixed
{
    public static class NetworkSerializer
    {
        public static byte[] Serialize<T>(T instance)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, instance);

                return stream.ToArray();
            }
        }

        public static T Deserialize<T>(byte[] data)
        {
            var target = Deserialize(data, typeof(T));

            return (T)target;
        }
        public static object Deserialize(byte[] data, Type type)
        {
            using (var stream = new MemoryStream(data))
            {
                var instance = Serializer.Deserialize(type, stream);

                return instance;
            }
        }

        static NetworkSerializer()
        {
            
        }
    }
}