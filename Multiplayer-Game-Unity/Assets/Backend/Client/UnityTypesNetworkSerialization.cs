using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using Backend.Shared;

namespace Backend
{
	public class UnityTypesNetworkSerialization
	{
        public const short Start = NetworkPayload.MinCode;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void OnLoad()
        {
            NetworkPayload.Register<Vector3>(Start + 1);
            NetworkPayload.Register<Quaternion>(Start + 2);
        }
	}

    public class Vector3SerializationResolver : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type) => type == typeof(Vector3);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = (Vector3)type;

            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            reader.Read(out float x);
            reader.Read(out float y);
            reader.Read(out float z);

            return new Vector3(x, y, z);
        }

        public Vector3SerializationResolver() { }
    }

    public class QuaternionNetworkSerializationResolver : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type) => type == typeof(Quaternion);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = (Quaternion)type;

            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            reader.Read(out float x);
            reader.Read(out float y);
            reader.Read(out float z);
            reader.Read(out float w);

            return new Quaternion(x, y, z, w);
        }

        public QuaternionNetworkSerializationResolver() { }
    }
}