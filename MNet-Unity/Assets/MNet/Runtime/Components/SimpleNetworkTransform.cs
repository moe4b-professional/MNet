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
    [AddComponentMenu(Constants.Path + "Simple Network Transform")]
    public class SimpleNetworkTransform : NetworkBehaviour
    {
        [SerializeField]
        float syncInverval = 0.1f;
        public float SyncInterval => syncInverval;

        [SerializeField]
        SyncConfigProperty syncConfig = default;
        public SyncConfigProperty SyncConfig => syncConfig;
        [Serializable]
        public class SyncConfigProperty : INetworkSerializable
        {
            [SerializeField]
            VectorProperty position = new VectorProperty(true);
            public VectorProperty Position => position;

            [SerializeField]
            VectorProperty rotation = new VectorProperty(false, true, false);
            public VectorProperty Rotation => rotation;

            [SerializeField]
            VectorProperty scale = new VectorProperty(false);
            public VectorProperty Scale => scale;

            [Serializable]
            public class VectorProperty : INetworkSerializable
            {
                [SerializeField]
                bool x = false;
                public bool X => x;

                [SerializeField]
                bool y = true;
                public bool Y => y;

                [SerializeField]
                bool z = false;
                public bool Z => z;

                public int Count
                {
                    get
                    {
                        var count = 0;

                        if (x) count += 1;
                        if (y) count += 1;
                        if (z) count += 1;

                        return count;
                    }
                }

                public int Size => Count * sizeof(float);

                public bool Any => x | y | z;

                public void Select(ref NetworkSerializationContext context)
                {
                    context.Select(ref x);
                    context.Select(ref y);
                    context.Select(ref z);
                }

                public VectorProperty(bool value) : this(value, value, value) { }
                public VectorProperty(bool x, bool y, bool z)
                {
                    this.x = x;
                    this.y = y;
                    this.z = z;
                }
            }

            public int Count => position.Count + rotation.Count + scale.Count;

            public int Size => Count * sizeof(float);

            public bool Any => position.Any | rotation.Any | scale.Any;

            public void Select(ref NetworkSerializationContext context)
            {
                context.Select(ref position);
                context.Select(ref rotation);
                context.Select(ref scale);
            }
        }

        NetworkWriter writer;
        NetworkReader reader;

        void Awake()
        {
            writer = new NetworkWriter(syncConfig.Size);
            reader = new NetworkReader();
        }

        void Start()
        {
            if(IsMine) StartCoroutine(Procedure());
        }

        IEnumerator Procedure()
        {
            while (true)
            {
                if (IsConnected) Broadcast();

                yield return new WaitForSeconds(syncInverval);
            }
        }

        void Broadcast()
        {
            WriteVector(transform.position, syncConfig.Position);
            WriteVector(transform.eulerAngles, syncConfig.Rotation);
            WriteVector(transform.localScale, syncConfig.Scale);

            var raw = writer.Flush();

            BroadcastRPC(Sync, raw, buffer: RpcBufferMode.Last, exception: Owner.ID);
        }

        void WriteVector(Vector3 value, SyncConfigProperty.VectorProperty config)
        {
            if (config.X) writer.Write(value.x);
            if (config.Y) writer.Write(value.y);
            if (config.Z) writer.Write(value.z);
        }

        [NetworkRPC(Authority = RemoteAuthority.Owner, Delivery = DeliveryMode.Unreliable)]
        void Sync(byte[] binary, RpcInfo info)
        {
            reader.Set(binary);

            transform.position = ReadVector(syncConfig.Position, transform.position);
            transform.eulerAngles = ReadVector(syncConfig.Rotation, transform.eulerAngles);
            transform.localScale = ReadVector(syncConfig.Scale, transform.localScale);
        }

        Vector3 ReadVector(SyncConfigProperty.VectorProperty config, Vector3 value)
        {
            if (config.X) value.x = reader.Read<float>();
            if (config.Y) value.y = reader.Read<float>();
            if (config.Z) value.z = reader.Read<float>();

            return value;
        }
    }
}