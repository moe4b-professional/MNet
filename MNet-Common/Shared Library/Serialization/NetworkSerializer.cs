using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Collections;
using System.Reflection;

namespace MNet
{
    public static class NetworkSerializer
    {
        public const int DefaultBufferSize = 512;

        public static byte[] Serialize<[NetworkSerializationGenerator] T>(T instance)
        {
            using (NetworkWriter.Pool.Lease(out var stream))
            {
                stream.Write(instance);

                return stream.ToArray();
            }
        }

        public static T Deserialize<[NetworkSerializationGenerator] T>(byte[] data)
        {
            using (NetworkReader.Pool.Lease(out var stream))
            {
                stream.Assign(data);
                return stream.Read<T>();
            }
        }

        public static T Clone<[NetworkSerializationGenerator] T>(T original)
        {
            using (NetworkStream.Pool.Lease(out var reader, out var writer))
            {
                writer.Write(original);
                reader.Assign(writer);

                return reader.Read<T>();
            }
        }
    }

    public interface INetworkSerializable
    {
        void Select(ref NetworkSerializationContext context);
    }

    public interface IManualNetworkSerializable
    {
        void Serialize(NetworkWriter writer);

        void Deserialize(NetworkReader reader);
    }
}