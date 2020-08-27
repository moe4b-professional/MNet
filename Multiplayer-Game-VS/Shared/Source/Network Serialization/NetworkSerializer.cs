using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Collections;
using System.Reflection;

namespace Backend
{
    public static class NetworkSerializer
    {
        public const int DefaultBufferSize = 2048;

        #region Serialize
        public static byte[] Serialize<T>(T instance) => Serialize(instance, DefaultBufferSize);
        public static byte[] Serialize<T>(T instance, int bufferSize)
        {
            using (var writer = new NetworkWriter(bufferSize))
            {
                writer.Write(instance);

                var result = writer.ToArray();

                return result;
            }
        }

        public static byte[] Serialize(object instance) => Serialize(instance, DefaultBufferSize);
        public static byte[] Serialize(object instance, int bufferSize)
        {
            using (var writer = new NetworkWriter(bufferSize))
            {
                writer.Write(instance);

                var result = writer.ToArray();

                return result;
            }
        }
        #endregion

        #region Deserialize
        public static T Deserialize<T>(byte[] data)
            where T : new()
        {
            using (var reader = new NetworkReader(data))
            {
                reader.Read(out T result);

                return result;
            }
        }

        public static object Deserialize(byte[] data, Type type)
        {
            using (var reader = new NetworkReader(data))
            {
                var result = reader.Read(type);

                return result;
            }
        }
        #endregion
    }

    public interface INetworkSerializable
    {
        void Serialize(NetworkWriter writer);

        void Deserialize(NetworkReader reader);
    }
}