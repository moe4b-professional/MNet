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
        NetworkEntitySyncTimer syncTimer = default;
        public NetworkEntitySyncTimer SyncTimer => syncTimer;

        [SerializeField]
        bool forceSync = false;
        public bool ForceSync => forceSync;

        [SerializeField]
        PositionProperty position = default;
        public PositionProperty Position => position;
        [Serializable]
        public class PositionProperty : Property<Vector3, VectorConstraint>
        {
            [SerializeField]
            VelocityProperty velocity = default;
            /// <summary>
            /// Only valid for remote clients, if you are a local client then you should get your velocity from somehwere else anyway
            /// </summary>
            public VelocityProperty Velocity => velocity;
            [Serializable]
            public class VelocityProperty
            {
                Queue<Vector3> samples;

                [SerializeField]
                int maxSamples = 5;

                public Vector3 Vector { get; protected set; }

                public float Magnitude => Vector.magnitude;

                internal void Add(Vector3 value)
                {
                    samples.Enqueue(value);

                    while (samples.Count > maxSamples) samples.Dequeue();

                    Vector = Vector3.zero;

                    foreach (var sample in samples)
                        Vector += sample;

                    Vector /= samples.Count;
                }

                public VelocityProperty()
                {
                    samples = new Queue<Vector3>();
                    Vector = Vector3.zero;
                }
            }

            public override Vector3 Value
            {
                get => Transform.Component.localPosition;
                protected set => Transform.Component.localPosition = value;
            }

            internal override void Translate()
            {
                var sample = Value;

                base.Translate();

                sample = (Value - sample) / Time.deltaTime;

                velocity.Add(sample);
            }

            protected override ChangeFlags ConstraintToChangeFlag(VectorConstraint constraints) => constraints.ToChangeFlag(AllFlags);

            protected override ChangeFlags CheckChanges(Vector3 previous, Vector3 current)
            {
                return ReadChanges(previous, current, AllFlags, ConstraintFlag, minChange);
            }

            protected override Vector3 Translate(Vector3 current, Vector3 target, float speed)
            {
                return Vector3.Lerp(current, target, speed * Time.deltaTime);
            }

            protected override void WriteTo(ref CoordinatesPacket packet, Vector3 value) => packet.SetPosition(value);

            protected override Vector3 ReadFrom(ref CoordinatesPacket packet, Vector3 source)
            {
                return ReadFrom(packet.Position, Value, AllFlags, packet.Changes);
            }

            public PositionProperty()
            {
                constraints = new VectorConstraint(true);

                AllFlags = new ChangeFlags[]
                {
                    ChangeFlags.PositionX,
                    ChangeFlags.PositionY,
                    ChangeFlags.PositionZ,
                };
            }
        }

        [SerializeField]
        RotationProperty rotation = default;
        public RotationProperty Rotation => rotation;
        [Serializable]
        public class RotationProperty : Property<Quaternion, VectorConstraint>
        {
            public override Quaternion Value
            {
                get => Transform.Component.localRotation;
                protected set => Transform.Component.localRotation = value;
            }

            protected override ChangeFlags ConstraintToChangeFlag(VectorConstraint constraints) => constraints.ToChangeFlag(AllFlags);

            protected override ChangeFlags CheckChanges(Quaternion previous, Quaternion current)
            {
                return ReadChanges(previous, current, AllFlags, ConstraintFlag, minChange);
            }

            protected override Quaternion Translate(Quaternion current, Quaternion target, float speed)
            {
                return Quaternion.Lerp(current, target, speed * Time.deltaTime);
            }

            protected override void WriteTo(ref CoordinatesPacket packet, Quaternion value) => packet.SetRotation(value.eulerAngles);

            protected override Quaternion ReadFrom(ref CoordinatesPacket packet, Quaternion source)
            {
                return ReadFrom(packet.Rotation, Value, AllFlags, packet.Changes);
            }

            public RotationProperty()
            {
                constraints = new VectorConstraint(true);

                AllFlags = new ChangeFlags[]
                {
                    ChangeFlags.RotationX,
                    ChangeFlags.RotationY,
                    ChangeFlags.RotationZ,
                };
            }
        }

        [SerializeField]
        ScaleProperty scale = default;
        public ScaleProperty Scale => scale;
        [Serializable]
        public class ScaleProperty : Property<Vector3, bool>
        {
            public override Vector3 Value
            {
                get => Transform.Component.localScale;
                protected set => Transform.Component.localScale = value;
            }

            protected override ChangeFlags ConstraintToChangeFlag(bool constraints) => constraints ? ChangeFlags.Scale : ChangeFlags.None;

            protected override ChangeFlags CheckChanges(Vector3 previous, Vector3 current)
            {
                return ReadChanges(previous, current, AllFlags, ConstraintFlag, minChange);
            }

            protected override Vector3 Translate(Vector3 current, Vector3 target, float speed)
            {
                return Vector3.Lerp(current, target, speed * Time.deltaTime);
            }

            protected override void WriteTo(ref CoordinatesPacket packet, Vector3 value) => packet.SetScale(value);

            protected override Vector3 ReadFrom(ref CoordinatesPacket packet, Vector3 source)
            {
                return ReadFrom(packet.Scale, Value, AllFlags, packet.Changes);
            }

            public ScaleProperty()
            {
                constraints = false;

                AllFlags = new ChangeFlags[]
                {
                    ChangeFlags.Scale,
                    ChangeFlags.Scale,
                    ChangeFlags.Scale,
                };
            }
        }

        [Serializable]
        public abstract class Property
        {
            public ChangeFlags[] AllFlags { get; protected set; }

            public ChangeFlags ConstraintFlag { get; protected set; }

            public SimpleNetworkTransform Transform { get; protected set; }
            internal virtual void Set(SimpleNetworkTransform reference)
            {
                Transform = reference;
            }

            #region Controls
            internal abstract ChangeFlags CheckChanges();

            internal abstract void Translate();
            internal abstract void Anchor();

            internal abstract void WriteTo(ref CoordinatesPacket packet);
            internal abstract void ReadFrom(ref CoordinatesPacket packet);
            #endregion

            //Static Utility
            #region Read Changes
            public static ChangeFlags ReadChanges(Vector3 previous, Vector3 current, ChangeFlags[] flags, ChangeFlags constraint, float epsilon)
            {
                var change = ChangeFlags.None;

                var delta = previous - current;

                delta = new Vector3()
                {
                    x = Mathf.Abs(delta.x),
                    y = Mathf.Abs(delta.y),
                    z = Mathf.Abs(delta.z),
                };

                if (constraint.HasFlag(flags[0])) if (delta.x >= epsilon) change |= flags[0];
                if (constraint.HasFlag(flags[1])) if (delta.y >= epsilon) change |= flags[1];
                if (constraint.HasFlag(flags[2])) if (delta.z >= epsilon) change |= flags[2];

                return change;
            }

            public static ChangeFlags ReadChanges(Quaternion previous, Quaternion current, ChangeFlags[] flags, ChangeFlags constraint, float epsilon)
            {
                return ReadChanges(previous.eulerAngles, current.eulerAngles, flags, constraint, epsilon);
            }
            #endregion

            #region Read From
            public static Vector3 ReadFrom(Vector3 target, Vector3 source, ChangeFlags[] flags, ChangeFlags change)
            {
                var copy = source;

                if (change.HasFlag(flags[0])) copy.x = target.x;
                if (change.HasFlag(flags[1])) copy.y = target.y;
                if (change.HasFlag(flags[2])) copy.z = target.z;

                return copy;
            }

            public static Quaternion ReadFrom(Vector3 target, Quaternion source, ChangeFlags[] flags, ChangeFlags change)
            {
                var vector = ReadFrom(target, source.eulerAngles, flags, change);

                return Quaternion.Euler(vector);
            }
            #endregion
        }

        public List<Property> Properties { get; protected set; }

        [Serializable]
        public abstract class Property<TValue, TConstraint> : Property
        {
            [SerializeField]
            protected float minChange = 0.05f;
            public float MinChange => minChange;

            [SerializeField]
            float speed = 10f;
            public float Speed => speed;

            [SerializeField]
            protected TConstraint constraints = default;
            public TConstraint Constraints => constraints;

            public TValue Target { get; protected set; }

            internal override void Set(SimpleNetworkTransform reference)
            {
                base.Set(reference);

                Target = Value;

                ConstraintFlag = ConstraintToChangeFlag(constraints);
            }

            #region Controls
            internal override ChangeFlags CheckChanges()
            {
                var changes = CheckChanges(Target, Value);

                return changes;
            }

            internal override void Translate()
            {
                Value = Translate(Value, Target, speed);
            }

            internal override void Anchor()
            {
                Value = Target;
            }

            internal override void WriteTo(ref CoordinatesPacket packet)
            {
                Target = Value;

                WriteTo(ref packet, Value);
            }

            internal override void ReadFrom(ref CoordinatesPacket packet)
            {
                Target = ReadFrom(ref packet, Value);
            }
            #endregion

            #region Abstractions
            public abstract TValue Value { get; protected set; }

            protected abstract ChangeFlags CheckChanges(TValue previous, TValue current);

            protected abstract ChangeFlags ConstraintToChangeFlag(TConstraint constraints);

            protected abstract TValue Translate(TValue current, TValue target, float speed);

            protected abstract void WriteTo(ref CoordinatesPacket packet, TValue value);
            protected abstract TValue ReadFrom(ref CoordinatesPacket packet, TValue source);
            #endregion
        }

        [Serializable]
        public class VectorConstraint
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

            public int BinarySize => Count * sizeof(float);

            public bool Any => x | y | z;

            public ChangeFlags ToChangeFlag(ChangeFlags[] flags)
            {
                var flag = ChangeFlags.None;

                if (x) flag |= flags[0];
                if (y) flag |= flags[1];
                if (z) flag |= flags[2];

                return flag;
            }

            public VectorConstraint(bool value) : this(value, value, value) { }
            public VectorConstraint(bool x, bool y, bool z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }

        [Flags]
        public enum ChangeFlags
        {
            None = 0,

            PositionX = 1 << 1,
            PositionY = 1 << 2,
            PositionZ = 1 << 3,

            RotationX = 1 << 4,
            RotationY = 1 << 5,
            RotationZ = 1 << 6,

            Scale = 1 << 7,
        }

        public struct CoordinatesPacket : INetworkSerializable
        {
            ChangeFlags changes;
            public ChangeFlags Changes => changes;

            #region Position
            float positionX;
            float positionY;
            float positionZ;

            public void SetPosition(Vector3 value)
            {
                positionX = value.x;
                positionY = value.y;
                positionZ = value.z;
            }

            public Vector3 Position => new Vector3(positionX, positionY, positionZ);
            #endregion

            #region Rotation
            float rotationX;
            float rotationY;
            float rotationZ;

            public void SetRotation(Vector3 value)
            {
                rotationX = value.x;
                rotationY = value.y;
                rotationZ = value.z;
            }

            public Vector3 Rotation => new Vector3(rotationX, rotationY, rotationZ);
            #endregion

            #region Scale
            Vector3 scale;
            public Vector3 Scale => scale;

            public void SetScale(Vector3 value)
            {
                scale = value;
            }
            #endregion

            public void Select(ref NetworkSerializationContext context)
            {
                context.Select(ref changes);

                if (changes.HasFlag(ChangeFlags.PositionX)) context.Select(ref positionX);
                if (changes.HasFlag(ChangeFlags.PositionY)) context.Select(ref positionY);
                if (changes.HasFlag(ChangeFlags.PositionZ)) context.Select(ref positionZ);

                if (changes.HasFlag(ChangeFlags.RotationX)) context.Select(ref rotationX);
                if (changes.HasFlag(ChangeFlags.RotationY)) context.Select(ref rotationY);
                if (changes.HasFlag(ChangeFlags.RotationZ)) context.Select(ref rotationZ);

                if (changes.HasFlag(ChangeFlags.Scale)) context.Select(ref scale);
            }

            public override string ToString() => $"( {changes} ):\n{Position} | {Rotation} | {scale}";

            public CoordinatesPacket(ChangeFlags changes, Vector3 position, Vector3 rotation, Vector3 scale)
            {
                this.changes = changes;

                positionX = position.x;
                positionY = position.y;
                positionZ = position.z;

                rotationX = rotation.x;
                rotationY = rotation.y;
                rotationZ = rotation.z;

                this.scale = scale;
            }

            public CoordinatesPacket(ChangeFlags changes) : this(changes, Vector3.zero, Vector3.zero, Vector3.zero) { }
        }

        public Transform Component => transform;

        protected override void Reset()
        {
            base.Reset();

#if UNITY_EDITOR
            syncTimer = NetworkEntitySyncTimer.Resolve(Entity);
#endif
        }

        void Awake()
        {
            Properties = new List<Property>() { position, rotation, scale };
        }

        void Start()
        {
            syncTimer.OnInvoke += Sync;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            if (Entity.IsMine && forceSync)
                Debug.LogWarning($"Force Sync is Enabled for {this}, This is Useful for Stress Testing but don't forget to turn it off");

            for (int i = 0; i < Properties.Count; i++)
                Properties[i].Set(this);
        }

        void Sync()
        {
            if (SendDelta() == false) return;

            SendBuffer();
        }

        bool SendDelta()
        {
            var changes = ChangeFlags.None;

            for (int i = 0; i < Properties.Count; i++)
                changes |= Properties[i].CheckChanges();

            if (forceSync)
            {
                for (int i = 0; i < Properties.Count; i++)
                    changes |= Properties[i].ConstraintFlag;
            }

            if (changes == ChangeFlags.None) return false;

            var coordinates = new CoordinatesPacket(changes);

            for (int i = 0; i < Properties.Count; i++)
                Properties[i].WriteTo(ref coordinates);

            BroadcastRPC(Delta, coordinates, delivery: DeliveryMode.Unreliable, exception: Entity.Owner);
            return true;
        }

        void SendBuffer()
        {
            var changes = ChangeFlags.None;

            for (int i = 0; i < Properties.Count; i++)
                changes |= Properties[i].ConstraintFlag;

            if (changes == ChangeFlags.None) return;

            var coordinates = new CoordinatesPacket(changes);

            for (int i = 0; i < Properties.Count; i++)
                Properties[i].WriteTo(ref coordinates);

            BufferRPC(Buffer, coordinates, delivery: DeliveryMode.ReliableSequenced);
        }

        void Update()
        {
            if (Entity.IsReady == false) return;

            if (Entity.IsMine == false) Translate();
        }

        void Translate()
        {
            for (int i = 0; i < Properties.Count; i++)
                Properties[i].Translate();
        }

        [NetworkRPC(Authority = RemoteAuthority.Owner)]
        void Delta(CoordinatesPacket coordinates, RpcInfo info)
        {
            Apply(ref coordinates, info.IsBuffered);
        }

        [NetworkRPC(Authority = RemoteAuthority.Owner)]
        void Buffer(CoordinatesPacket coordinates, RpcInfo info)
        {
            Apply(ref coordinates, info.IsBuffered);
        }

        void Apply(ref CoordinatesPacket coordinates, bool anchor)
        {
            for (int i = 0; i < Properties.Count; i++)
                Properties[i].ReadFrom(ref coordinates);

            if (anchor)
            {
                for (int i = 0; i < Properties.Count; i++)
                    Properties[i].Anchor();
            }
        }
    }
}