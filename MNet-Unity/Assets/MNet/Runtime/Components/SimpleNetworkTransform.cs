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
        [SyncInterval(0, 200)]
        [Tooltip("Sync Interval in ms, 1s = 1000ms")]
        int syncInverval = 100;
        public int SyncInterval => syncInverval;

        [SerializeField]
        VectorCoordinateProperty position = new VectorCoordinateProperty(true);
        public VectorCoordinateProperty Position => position;
        
        [SerializeField]
        QuaternionCoordinateProperty rotation = new QuaternionCoordinateProperty();
        public QuaternionCoordinateProperty Rotation => rotation;

        [SerializeField]
        VectorCoordinateProperty scale = new VectorCoordinateProperty(false);
        public VectorCoordinateProperty Scale => scale;

        [SerializeField]
        bool forceSync = false;
        public bool ForceSync => forceSync;

        [Serializable]
        public class VectorCoordinateProperty : CoordinateProperty<Vector3>
        {
            public override float CalculateDistance(Vector3 target) => Vector3.Distance(value, target);

            public override Vector3 MoveTowards(Vector3 current) => Vector3.Lerp(current, value, speed * Time.deltaTime);

            public override void WriteBinary(NetworkWriter writer)
            {
                if (Target.X) writer.Write(value.x);
                if (Target.Y) writer.Write(value.y);
                if (Target.Z) writer.Write(value.z);
            }
            public override void ReadBinary(NetworkReader reader)
            {
                if (Target.X) value.x = reader.Read<float>();
                if (Target.Y) value.y = reader.Read<float>();
                if (Target.Z) value.z = reader.Read<float>();
            }

            public VectorCoordinateProperty(bool target)
            {
                this.target = new VectorTargetProperty(target);
            }
        }

        [Serializable]
        public class QuaternionCoordinateProperty : CoordinateProperty<Quaternion>
        {
            public override float CalculateDistance(Quaternion target) => Quaternion.Angle(value, target);

            public override Quaternion MoveTowards(Quaternion current) => Quaternion.Lerp(current, value, speed * Time.deltaTime);

            public override void WriteBinary(NetworkWriter writer)
            {
                var vector = Value.eulerAngles;

                if (Target.X) writer.Write(vector.x);
                if (Target.Y) writer.Write(vector.y);
                if (Target.Z) writer.Write(vector.z);
            }
            public override void ReadBinary(NetworkReader reader)
            {
                var vector = value.eulerAngles;

                if (Target.X) vector.x = reader.Read<float>();
                if (Target.Y) vector.y = reader.Read<float>();
                if (Target.Z) vector.z = reader.Read<float>();

                value = Quaternion.Euler(vector);
            }

            public QuaternionCoordinateProperty()
            {
                target = new VectorTargetProperty(false, true, false);
            }
        }

        [Serializable]
        public abstract class CoordinateProperty<TValue>
        {
            protected TValue value = default;
            public TValue Value
            {
                get => value;
                set => Set(value);
            }

            [SerializeField]
            protected VectorTargetProperty target;
            public VectorTargetProperty Target => target;

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
                if (this.target.Any == false) return false;

                if (CalculateDistance(target) < epsilon) return false;

                value = target;
                return true;
            }

            public abstract void WriteBinary(NetworkWriter writer);
            public abstract void ReadBinary(NetworkReader reader);
        }

        [Serializable]
        public class VectorTargetProperty
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

            public VectorTargetProperty(bool value) : this(value, value, value) { }
            public VectorTargetProperty(bool x, bool y, bool z)
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
            writer = new NetworkWriter(position.Target.Size + rotation.Target.Size + scale.Target.Size);
            reader = new NetworkReader();

            position.Set(transform.localPosition);
            rotation.Set(transform.localRotation);
            scale.Set(transform.localScale);
        }

        void Start()
        {
            StartCoroutine(Procedure());
        }

        //Yes, I'm using coroutines, get off my back!
        IEnumerator Procedure()
        {
            while (true)
            {
                if (Entity.IsMine)
                    yield return LocalProcedure();
                else
                    yield return RemoteProcedure();
            }
        }

        YieldInstruction LocalProcedure()
        {
            if (Entity.IsConnected)
            {
                bool updated = false;

                updated |= position.Update(transform.localPosition);
                updated |= rotation.Update(transform.localRotation);
                updated |= scale.Update(transform.localScale);

                if (updated || forceSync) Broadcast();
            }

            return new WaitForSeconds(syncInverval / 1000f);
        }

        YieldInstruction RemoteProcedure()
        {
            if (position.Target.Any) transform.localPosition = position.MoveTowards(transform.localPosition);
            if (rotation.Target.Any) transform.localRotation = rotation.MoveTowards(transform.localRotation);
            if (scale.Target.Any) transform.localScale = scale.MoveTowards(transform.localScale);

            return new WaitForEndOfFrame();
        }

        void Broadcast()
        {
            position.WriteBinary(writer);
            rotation.WriteBinary(writer);
            scale.WriteBinary(writer);

            var raw = writer.Flush();

            BroadcastRPC(Sync, raw, buffer: RemoteBufferMode.Last, exception: Entity.Owner);
        }

        [NetworkRPC(Authority = RemoteAuthority.Owner | RemoteAuthority.Master, Delivery = DeliveryMode.Unreliable)]
        void Sync(byte[] binary, RpcInfo info)
        {
            reader.Set(binary);

            position.ReadBinary(reader);
            rotation.ReadBinary(reader);
            scale.ReadBinary(reader);

            if(info.IsBuffered)
            {
                transform.localPosition = position.Value;
                transform.localRotation = rotation.Value;
                transform.localScale = scale.Value;
            }
        }
    }
}