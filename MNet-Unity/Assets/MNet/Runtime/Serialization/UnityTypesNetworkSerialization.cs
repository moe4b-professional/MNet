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
    public class UnityTypesNetworkSerialization
    {
        public static class IDs
        {
            public const ushort Start = NetworkPayload.MinCode;

            public const ushort Vector2 = Start + 1;
            public const ushort Vector2Int = Vector2 + 1;

            public const ushort Vector3 = Vector2Int + 1;
            public const ushort Vector3Int = Vector3 + 1;

            public const ushort Vector4 = Vector3Int + 1;

            public const ushort Quaternion = Vector4 + 1;

            public const ushort Color = Quaternion + 1;

            public const ushort NetworkEntity = Color + 1;
            public const ushort NetworkBehaviour = NetworkEntity + 1;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void OnLoad()
        {
            NetworkPayload.Register<Vector2>(IDs.Vector2);
            NetworkPayload.Register<Vector2Int>(IDs.Vector2Int);

            NetworkPayload.Register<Vector3>(IDs.Vector3);
            NetworkPayload.Register<Vector3Int>(IDs.Vector3Int);

            NetworkPayload.Register<Vector4>(IDs.Vector4);

            NetworkPayload.Register<Quaternion>(IDs.Quaternion);

            NetworkPayload.Register<Color>(IDs.Color);

            NetworkPayload.Register<NetworkEntity>(IDs.NetworkEntity, true);
            NetworkPayload.Register<NetworkBehaviour>(IDs.NetworkBehaviour, true);
        }
    }

    #region Vector2
    [Preserve]
    public class Vector2SerializationResolver : NetworkSerializationExplicitResolver<Vector2>
    {
        public override void Serialize(NetworkWriter writer, Vector2 instance)
        {
            writer.Write(instance.x);
            writer.Write(instance.y);
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
        public override void Serialize(NetworkWriter writer, Vector2Int instance)
        {
            writer.Write(instance.x);
            writer.Write(instance.y);
        }

        public override Vector2Int Deserialize(NetworkReader reader)
        {
            reader.Read(out int x);
            reader.Read(out int y);

            return new Vector2Int(x, y);
        }
    }
    #endregion

    #region Vector3
    [Preserve]
    public class Vector3SerializationResolver : NetworkSerializationExplicitResolver<Vector3>
    {
        public override void Serialize(NetworkWriter writer, Vector3 instance)
        {
            writer.Write(instance.x);
            writer.Write(instance.y);
            writer.Write(instance.z);
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
        public override void Serialize(NetworkWriter writer, Vector3Int instance)
        {
            writer.Write(instance.x);
            writer.Write(instance.y);
            writer.Write(instance.z);
        }

        public override Vector3Int Deserialize(NetworkReader reader)
        {
            reader.Read(out int x);
            reader.Read(out int y);
            reader.Read(out int z);

            return new Vector3Int(x, y, z);
        }
    }
    #endregion

    #region Vector4
    [Preserve]
    public class Vector4SerializationResolver : NetworkSerializationExplicitResolver<Vector4>
    {
        public override void Serialize(NetworkWriter writer, Vector4 instance)
        {
            writer.Write(instance.x);
            writer.Write(instance.y);
            writer.Write(instance.z);
            writer.Write(instance.w);
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
        public override void Serialize(NetworkWriter writer, Color instance)
        {
            writer.Write(instance.r);
            writer.Write(instance.g);
            writer.Write(instance.b);
            writer.Write(instance.a);
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
        public override void Serialize(NetworkWriter writer, Quaternion instance)
        {
            writer.Write(instance.x);
            writer.Write(instance.y);
            writer.Write(instance.z);
            writer.Write(instance.w);
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
    public class NetworkEntityNetworkSerializationResolver : NetworkSerializationExplicitResolver<NetworkEntity>
    {
        public override bool CanResolveDerivatives => true;

        enum State : byte
        {
            Null, Unready, Ready
        }

        public override void Serialize(NetworkWriter writer, NetworkEntity instance)
        {
            if (instance == null)
            {
                writer.Write(State.Null);
                return;
            }

            if (instance.IsReady == false)
            {
                writer.Write(State.Unready);
                Debug.LogError($"Trying to Serialize Unready Entity {instance}, Please only Send Ready Entites Over The Network, Reciever will deserializer as null");
                return;
            }

            writer.Write(State.Ready);
            writer.Write(instance.ID);
        }

        public override NetworkEntity Deserialize(NetworkReader reader)
        {
            reader.Read(out State state);

            if (state == State.Null || state == State.Unready)
                return null;

            reader.Read(out NetworkEntityID id);

            if (NetworkEntity.TryFind(id, out var entity) == false)
            {
                Debug.LogWarning($"Network Entity {id} Couldn't be Found when Deserializing, Returning null");
                return null;
            }

            return entity;
        }
    }

    [Preserve]
    public class NetworkBehaviourNetworkSerializationResolver : NetworkSerializationExplicitResolver<NetworkBehaviour>
    {
        public override bool CanResolveDerivatives => true;

        public override void Serialize(NetworkWriter writer, NetworkBehaviour instance)
        {
            writer.Write(instance.Entity);
            writer.Write(instance.ID);
        }

        public override NetworkBehaviour Deserialize(NetworkReader reader)
        {
            reader.Read(out NetworkEntity entity);
            reader.Read(out NetworkBehaviourID id);

            if (entity == null) return null;

            if (entity.Behaviours.TryGetValue(id, out var behaviour) == false)
            {
                Debug.LogWarning($"Network Behaviour {id} Couldn't be Found on Entity '{entity}' when Deserializing, Returning null");
                return null;
            }

            return behaviour;
        }
    }
}