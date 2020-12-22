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
        ConfigProperty config = default;
        public ConfigProperty Config => config;
        [Serializable]
        public class ConfigProperty : INetworkSerializable
        {
            [SerializeField]
            bool position = true;
            public bool Position => position;

            [SerializeField]
            RotationProperty rotation = new RotationProperty();
            public RotationProperty Rotation => rotation;
            [Serializable]
            public class RotationProperty : INetworkSerializable
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

                public bool Any => x | y | z;

                public void Select(ref NetworkSerializationContext context)
                {
                    context.Select(ref x);
                    context.Select(ref y);
                    context.Select(ref z);
                }
            }

            [SerializeField]
            bool scale = false;
            public bool Scale => scale;

            public int Count
            {
                get
                {
                    var count = 0;

                    if (position) count += 1;

                    if (rotation.X) count += 1;
                    if (rotation.Y) count += 1;
                    if (rotation.Z) count += 1;

                    if (scale) count += 1;

                    return count;
                }
            }

            public int Size => Count * sizeof(float);

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
            writer = new NetworkWriter(config.Size);
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
            var raw = WriteBinary();

            BroadcastRPC(Sync, raw, buffer: RpcBufferMode.Last, exception: Owner.ID);
        }

        byte[] WriteBinary()
        {
            if (config.Position) writer.Write(transform.position);

            if (config.Rotation.X) writer.Write(transform.eulerAngles.x);
            if (config.Rotation.Y) writer.Write(transform.eulerAngles.y);
            if (config.Rotation.Z) writer.Write(transform.eulerAngles.z);

            if (config.Scale) writer.Write(transform.localScale);

            var raw = writer.ToArray();

            writer.Clear();

            return raw;
        }

        [NetworkRPC(Authority = RemoteAuthority.Owner, Delivery = DeliveryMode.Unreliable)]
        void Sync(byte[] binary, RpcInfo info) => ReadBinary(config, binary);

        void ReadBinary(ConfigProperty config, byte[] binary)
        {
            reader.Set(binary);

            if (config.Position)
            {
                reader.Read(out Vector3 value);
                transform.position = value;
            }

            if (config.Rotation.Any)
            {
                var angles = transform.eulerAngles;

                if (config.Rotation.X) angles.x = reader.Read<float>();
                if (config.Rotation.Y) angles.y = reader.Read<float>();
                if (config.Rotation.Z) angles.z = reader.Read<float>();

                transform.eulerAngles = angles;
            }

            if (config.Scale)
            {
                reader.Read(out Vector3 value);
                transform.localScale = value;
            }
        }
    }
}