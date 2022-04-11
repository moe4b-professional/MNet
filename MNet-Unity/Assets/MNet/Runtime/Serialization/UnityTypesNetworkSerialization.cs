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
    public static class UnityTypesNetworkSerialization
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        static void OnLoad()
        {
            DynamicNetworkSerialization.Register(DynamicNetworkBehaviour);
            DynamicNetworkSerialization.Register(DynamicISyncedAsset);
        }

        static bool DynamicISyncedAsset(Type type, ref NetworkSerializationResolver resolver)
        {
            if (typeof(ISyncedAsset).IsAssignableFrom(type) == false)
                return false;

            resolver = DynamicNetworkSerialization.ConstructResolver(typeof(SyncedAssetNetworkSerializationResolver<>), type);
            return true;
        }
        static bool DynamicNetworkBehaviour(Type type, ref NetworkSerializationResolver resolver)
        {
            if (typeof(INetworkBehaviour).IsAssignableFrom(type) == false)
                return false;

            resolver = DynamicNetworkSerialization.ConstructResolver(typeof(NetworkBehaviourNetworkSerializationResolver<>), type);
            return true;
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
        public enum State : byte
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
    public class NetworkBehaviourNetworkSerializationResolver<T> : NetworkSerializationExplicitResolver<T>
        where T : class, INetworkBehaviour
    {
        public override void Serialize(NetworkWriter writer, T instance)
        {
            writer.Write(instance.Network.Entity);
            writer.Write(instance.Network.ID);
        }
        public override T Deserialize(NetworkReader reader)
        {
            reader.Read(out NetworkEntity entity);
            reader.Read(out NetworkBehaviourID id);

            if (entity == null)
                return null;

            if (entity.Behaviours.Dictionary.TryGetValue(id, out var behaviour) == false)
            {
                Debug.LogWarning($"Network Behaviour {id} Couldn't be Found on Entity '{entity}' when Deserializing, Returning null");
                return null;
            }

            return behaviour.Contract as T;
        }
    }

    [Preserve]
    public class SyncedAssetNetworkSerializationResolver<T> : NetworkSerializationExplicitResolver<T>
        where T : class, ISyncedAsset
    {
        public override void Serialize(NetworkWriter writer, T instance)
        {
            var asset = instance as Object;

            if (NetworkAPI.Config.SyncedAssets.TryGetIndex(asset, out var index) == false)
                throw new Exception($"Cannot Serialize Unregistered Synced Asset {asset}");

            writer.Write(index);
        }
        public override T Deserialize(NetworkReader reader)
        {
            reader.Read(out ushort index);

            var asset = NetworkAPI.Config.SyncedAssets[index] as ISyncedAsset;

            if (asset == null)
                Debug.LogWarning($"No Synced Asset found for index {index} when Deserializing");

            return asset as T;
        }
    }
}