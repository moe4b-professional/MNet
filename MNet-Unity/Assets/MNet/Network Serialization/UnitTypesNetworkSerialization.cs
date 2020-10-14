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

namespace MNet
{
    [Preserve]
    public class UnitTypesNetworkSerialization
	{
        public static class IDs
        {
            public const ushort Start = NetworkPayload.MinCode;

            public const ushort Vector3 = Start + 1;
            public const ushort Quaternion = Vector3 + 1;
            public const ushort Vector2 = Quaternion + 1;
            public const ushort NetworkEntity = Vector2 + 1;
            public const ushort NetworkBehaviour = NetworkEntity + 1;
            public const ushort Vector4 = NetworkBehaviour + 1;
            public const ushort Vector2Int = Vector4 + 1;
            public const ushort Vector3Int = Vector2Int + 1;
            public const ushort Color = Vector3Int + 1;
        }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void OnLoad()
        {
            NetworkPayload.Register<Quaternion>(IDs.Quaternion);

            NetworkPayload.Register<Vector2>(IDs.Vector2);
            NetworkPayload.Register<Vector2Int>(IDs.Vector2Int);
            NetworkPayload.Register<Vector3>(IDs.Vector3);
            NetworkPayload.Register<Vector3Int>(IDs.Vector3Int);
            NetworkPayload.Register<Vector4>(IDs.Vector4);

            NetworkPayload.Register<Color>(IDs.Color);

            NetworkPayload.Register<NetworkEntity>(IDs.NetworkEntity, true);
            NetworkPayload.Register<NetworkBehaviour>(IDs.NetworkBehaviour, true);
        }
	}

    #region Vector
    [Preserve]
    public class Vector2SerializationResolver : NetworkSerializationExplicitResolver<Vector2>
    {
        public override void Serialize(NetworkWriter writer, Vector2 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public override Vector2 Deserialize(NetworkReader reader)
        {
            reader.Read(out float x);
            reader.Read(out float y);

            return new Vector2(x, y);
        }
    }
    [Preserve]
    public class Vector2IntSerializationResolver : NetworkSerializationExplicitResolver<Vector2Int>
    {
        public override void Serialize(NetworkWriter writer, Vector2Int value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public override Vector2Int Deserialize(NetworkReader reader)
        {
            reader.Read(out int x);
            reader.Read(out int y);

            return new Vector2Int(x, y);
        }
    }

    [Preserve]
    public class Vector3SerializationResolver : NetworkSerializationExplicitResolver<Vector3>
    {
        public override void Serialize(NetworkWriter writer, Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public override Vector3 Deserialize(NetworkReader reader)
        {
            reader.Read(out float x);
            reader.Read(out float y);
            reader.Read(out float z);

            return new Vector3(x, y, z);
        }
    }
    [Preserve]
    public class Vector3IntSerializationResolver : NetworkSerializationExplicitResolver<Vector3Int>
    {
        public override void Serialize(NetworkWriter writer, Vector3Int value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public override Vector3Int Deserialize(NetworkReader reader)
        {
            reader.Read(out int x);
            reader.Read(out int y);
            reader.Read(out int z);

            return new Vector3Int(x, y, z);
        }
    }

    [Preserve]
    public class Vector4SerializationResolver : NetworkSerializationExplicitResolver<Vector4>
    {
        public override void Serialize(NetworkWriter writer, Vector4 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        public override Vector4 Deserialize(NetworkReader reader)
        {
            reader.Read(out float x);
            reader.Read(out float y);
            reader.Read(out float z);
            reader.Read(out float w);

            return new Vector4(x, y, z, w);
        }
    }
    #endregion

    [Preserve]
    public class ColorSerializationResolver : NetworkSerializationExplicitResolver<Color>
    {
        public override void Serialize(NetworkWriter writer, Color value)
        {
            writer.Write(value.r);
            writer.Write(value.g);
            writer.Write(value.b);
            writer.Write(value.a);
        }

        public override Color Deserialize(NetworkReader reader)
        {
            reader.Read(out float r);
            reader.Read(out float g);
            reader.Read(out float b);
            reader.Read(out float a);

            return new Color(r, g, b, a);
        }
    }

    [Preserve]
    public class QuaternionNetworkSerializationResolver : NetworkSerializationExplicitResolver<Quaternion>
    {
        public override void Serialize(NetworkWriter writer, Quaternion value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        public override Quaternion Deserialize(NetworkReader reader)
        {
            reader.Read(out float x);
            reader.Read(out float y);
            reader.Read(out float z);
            reader.Read(out float w);

            return new Quaternion(x, y, z, w);
        }
    }

    [Preserve]
    public class NetworkEntityNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static Type Class { get; protected set; } = typeof(NetworkEntity);

        public override bool CanResolve(Type type) => Class.IsAssignableFrom(type);

        public override void Serialize(NetworkWriter writer, object instance)
        {
            var entity = instance as NetworkEntity;

            Serialize(writer, entity);
        }
        public static bool Serialize(NetworkWriter writer, NetworkEntity entity)
        {
            writer.Write(entity.IsReady);

            if (entity.IsReady)
            {
                writer.Write(entity.ID);
                return true;
            }
            else
            {
                Debug.LogError("Trying to Serialize Unready Network Entity Across The Network, Recievers Will Deserialize as Null");
                return false;
            }
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            Deserialize(reader, out var entity);

            return entity;
        }
        public static bool Deserialize(NetworkReader reader, out NetworkEntity entity)
        {
            reader.Read(out bool isReady);

            if (isReady == false)
            {
                entity = null;
                return false;
            }

            reader.Read(out NetworkEntityID id);

            if (NetworkAPI.Room.Entities.TryGetValue(id, out entity) == false)
            {
                Debug.LogWarning($"Network Entity {id} Couldn't be Found when Deserializing, Returning null");
                return false;
            }

            return true;
        }
    }

    [Preserve]
    public class NetworkBehaviourNetworkSerializationResolver : NetworkSerializationImplicitResolver
    {
        public static Type Class { get; protected set; } = typeof(NetworkBehaviour);

        public override bool CanResolve(Type type) => Class.IsAssignableFrom(type);

        public override void Serialize(NetworkWriter writer, object instance)
        {
            var behaviour = instance as NetworkBehaviour;

            if (NetworkEntityNetworkSerializationResolver.Serialize(writer, behaviour.Entity) == false) return;

            writer.Write(behaviour.ID);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            if (NetworkEntityNetworkSerializationResolver.Deserialize(reader, out var entity) == false) return null;

            reader.Read(out NetworkBehaviourID behaviourID);

            if(entity.TryGetBehaviour(behaviourID, out var behaviour) == false)
            {
                Debug.LogWarning($"Network Behaviour {behaviourID} Couldn't be Found on Entity '{entity}' when Deserializing, Returning null");
                return null;
            }

            var value = Convert.ChangeType(behaviour, type);

            return behaviour;
        }
    }
}