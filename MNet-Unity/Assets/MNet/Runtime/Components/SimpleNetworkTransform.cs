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
        VectorCoordinateProperty position = default;
        public VectorCoordinateProperty Position => position;
        [Serializable]
        public class VectorCoordinateProperty : CoordinateProperty<Vector3>
        {
            public override float CalculateDistance(Vector3 target) => Vector3.Distance(value, target);

            public override Vector3 MoveTowards(Vector3 current) => Vector3.Lerp(current, value, speed * Time.deltaTime);

            public override void WriteBinary(NetworkWriter writer)
            {
                if (ReplicationTarget.X) writer.Write(value.x);
                if (ReplicationTarget.Y) writer.Write(value.y);
                if (ReplicationTarget.Z) writer.Write(value.z);
            }
            public override void ReadBinary(NetworkReader reader)
            {
                if (ReplicationTarget.X) value.x = reader.Read<float>();
                if (ReplicationTarget.Y) value.y = reader.Read<float>();
                if (ReplicationTarget.Z) value.z = reader.Read<float>();
            }

            public VectorCoordinateProperty()
            {
                replicationTarget = new ReplicationTargetProperty(true);
            }
        }

        [SerializeField]
        QuaternionCoordinateProperty rotation = default;
        public QuaternionCoordinateProperty Rotation => rotation;
        [Serializable]
        public class QuaternionCoordinateProperty : CoordinateProperty<Quaternion>
        {
            public override float CalculateDistance(Quaternion target) => Quaternion.Angle(value, target);

            public override Quaternion MoveTowards(Quaternion current) => Quaternion.Lerp(current, value, speed * Time.deltaTime);

            public override void WriteBinary(NetworkWriter writer)
            {
                var vector = Value.eulerAngles;

                if (ReplicationTarget.X) writer.Write(vector.x);
                if (ReplicationTarget.Y) writer.Write(vector.y);
                if (ReplicationTarget.Z) writer.Write(vector.z);
            }
            public override void ReadBinary(NetworkReader reader)
            {
                var vector = value.eulerAngles;

                if (ReplicationTarget.X) vector.x = reader.Read<float>();
                if (ReplicationTarget.Y) vector.y = reader.Read<float>();
                if (ReplicationTarget.Z) vector.z = reader.Read<float>();

                value = Quaternion.Euler(vector);
            }

            public QuaternionCoordinateProperty()
            {
                replicationTarget = new ReplicationTargetProperty(false, true, false);
            }
        }

        [Serializable]
        public abstract class CoordinateProperty<TValue>
        {
            [SerializeField]
            protected TValue value = default;
            public TValue Value
            {
                get => value;
                set => Set(value);
            }

            [SerializeField]
            protected ReplicationTargetProperty replicationTarget = default;
            public ReplicationTargetProperty ReplicationTarget => replicationTarget;

            [SerializeField]
            protected float epsilon = 0.01f;
            public float Epsilon => epsilon;

            [SerializeField]
            protected float speed = 10;
            public float Speed => speed;

            public void Set(TValue value) => this.value = value;

            public abstract TValue MoveTowards(TValue current);

            public abstract float CalculateDistance(TValue target);

            public bool Update(TValue target)
            {
                if (replicationTarget.Any == false) return false;

                if (CalculateDistance(target) < epsilon) return false;

                value = target;
                return true;
            }

            public abstract void WriteBinary(NetworkWriter writer);
            public abstract void ReadBinary(NetworkReader reader);
        }

        [Serializable]
        public class ReplicationTargetProperty
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

            public ReplicationTargetProperty(bool value) : this(value, value, value) { }
            public ReplicationTargetProperty(bool x, bool y, bool z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }

        NetworkWriter writer;
        NetworkReader reader;

        void Awake()
        {
            writer = new NetworkWriter(position.ReplicationTarget.Size + rotation.ReplicationTarget.Size);
            reader = new NetworkReader();
        }

        void Start()
        {
            StartCoroutine(Procedure());
        }

        IEnumerator Procedure()
        {
            while (true)
            {
                if(IsMine)
                {
                    if (IsConnected)
                    {
                        bool updated = false;

                        updated |= position.Update(transform.position);
                        updated |= rotation.Update(transform.rotation);

                        if (updated) Broadcast();
                    }

                    yield return new WaitForSeconds(syncInverval);
                }
                else
                {
                    transform.position = Position.MoveTowards(transform.position);
                    transform.rotation = Rotation.MoveTowards(transform.rotation);

                    yield return new WaitForEndOfFrame();
                }
            }
        }

        void Broadcast()
        {
            position.WriteBinary(writer);
            rotation.WriteBinary(writer);

            var raw = writer.Flush();

            BroadcastRPC(Sync, raw, buffer: RpcBufferMode.Last, exception: Owner.ID);
        }

        [NetworkRPC(Authority = RemoteAuthority.Owner, Delivery = DeliveryMode.Unreliable)]
        void Sync(byte[] binary, RpcInfo info)
        {
            reader.Set(binary);

            Position.ReadBinary(reader);
            Rotation.ReadBinary(reader);

            if(info.IsBuffered)
            {
                transform.position = position.Value;
                transform.rotation = rotation.Value;
            }
        }
    }
}