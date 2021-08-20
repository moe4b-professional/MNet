﻿using System;
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

        #region Serialize
        public static byte[] Serialize<T>(T instance)
        {
            using (var stream = NetworkStream.Pool.Any)
            {
                stream.Write(instance);

                return stream.ToArray();
            }
        }

        public static byte[] Serialize(object instance)
        {
            var type = instance == null ? null : instance.GetType();

            return Serialize(instance, type);
        }
        public static byte[] Serialize(object instance, Type type)
        {
            using (var stream = NetworkStream.Pool.Any)
            {
                stream.Write(instance, type);

                return stream.ToArray();
            }
        }
        #endregion

        #region Deserialize
        public static T Deserialize<T>(byte[] data)
        {
            var reader = new NetworkStream(data);

            reader.Read(out T result);

            return result;
        }

        public static object Deserialize(byte[] data, Type type)
        {
            var reader = new NetworkStream(data);

            var result = reader.Read(type);

            return result;
        }
        #endregion

        public static T Clone<T>(T original)
        {
            var binary = Serialize(original);

            var instance = Deserialize<T>(binary);

            return instance;
        }

        public static object Clone(object original, Type type)
        {
            var binary = Serialize(original, type);

            var instance = Deserialize(binary, type);

            return instance;
        }
    }

    public interface INetworkSerializable
    {
        void Select(ref NetworkSerializationContext context);
    }

    public interface IManualNetworkSerializable
    {
        void Serialize(NetworkStream writer);

        void Deserialize(NetworkStream reader);
    }
}