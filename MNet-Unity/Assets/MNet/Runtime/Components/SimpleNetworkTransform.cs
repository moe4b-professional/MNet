﻿using System;
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

            protected override float CalculateDelta(Vector3 a, Vector3 b) => Vector3.Distance(a, b);

            internal override void Translate()
            {
                var sample = Value;

                base.Translate();

                sample = (Value - sample) / Time.deltaTime;

                velocity.Add(sample);
            }

            protected override Vector3 Translate(Vector3 current, Vector3 target, float speed)
            {
                return Vector3.Lerp(current, target, speed * Time.deltaTime);
            }

            protected override void WriteBinary(NetworkWriter writer, Vector3 value)
            {
                WriteBinary(writer, value, constraints);
            }
            protected override Vector3 ReadBinary(NetworkReader reader, Vector3 source)
            {
                return ReadBinary(reader, source, constraints);
            }

            public PositionProperty()
            {
                constraints = new VectorConstraint(true);
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

            protected override float CalculateDelta(Quaternion a, Quaternion b) => Quaternion.Angle(a, b);

            protected override Quaternion Translate(Quaternion current, Quaternion target, float speed)
            {
                return Quaternion.Lerp(current, target, speed * Time.deltaTime);
            }

            protected override void WriteBinary(NetworkWriter writer, Quaternion value)
            {
                WriteBinary(writer, value, constraints);
            }
            protected override Quaternion ReadBinary(NetworkReader reader, Quaternion source)
            {
                return ReadBinary(reader, source, constraints);
            }

            public RotationProperty()
            {
                constraints = new VectorConstraint(false, true, false);
            }
        }

        [SerializeField]
        ScaleProperty scale = default;
        public ScaleProperty Scale => scale;
        [Serializable]
        public class ScaleProperty : Property<Vector3, VectorConstraint>
        {
            public override Vector3 Value
            {
                get => Transform.Component.localScale;
                protected set => Transform.Component.localScale = value;
            }

            protected override float CalculateDelta(Vector3 a, Vector3 b) => Vector3.Distance(a, b);

            protected override Vector3 Translate(Vector3 current, Vector3 target, float speed)
            {
                return Vector3.Lerp(current, target, speed * Time.deltaTime);
            }

            protected override void WriteBinary(NetworkWriter writer, Vector3 value)
            {
                WriteBinary(writer, value, constraints);
            }
            protected override Vector3 ReadBinary(NetworkReader reader, Vector3 source)
            {
                return ReadBinary(reader, source, constraints);
            }

            public ScaleProperty()
            {
                constraints = new VectorConstraint(false);
            }
        }

        #region Properties
        [Serializable]
        public abstract class Property
        {
            public SimpleNetworkTransform Transform { get; protected set; }
            internal virtual void Set(SimpleNetworkTransform reference)
            {
                Transform = reference;
            }

            #region Controls
            internal abstract bool CheckChanges();

            internal abstract void Update(NetworkWriter writer);

            internal abstract void Sync(NetworkReader reader);

            internal abstract void Translate();

            internal abstract void Anchor();
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
            }

            #region Controls
            internal override bool CheckChanges()
            {
                var previous = Target;
                var current = Value;

                var delta = CalculateDelta(previous, current);

                if (delta < minChange) return false;

                return true;
            }

            internal override void Update(NetworkWriter writer)
            {
                Target = Value;

                WriteBinary(writer, Target);
            }

            internal override void Sync(NetworkReader reader)
            {
                Target = ReadBinary(reader, Value);
            }

            internal override void Translate()
            {
                Value = Translate(Value, Target, speed);
            }

            internal override void Anchor()
            {
                Value = Target;
            }
            #endregion

            #region Abstractions
            public abstract TValue Value { get; protected set; }

            protected abstract TValue Translate(TValue current, TValue target, float speed);

            protected abstract float CalculateDelta(TValue a, TValue b);

            protected abstract void WriteBinary(NetworkWriter writer, TValue value);
            protected abstract TValue ReadBinary(NetworkReader reader, TValue source);
            #endregion

            //Static Utility
            #region Write Binary
            public static void WriteBinary(NetworkWriter writer, Vector3 value, VectorConstraint constraints)
            {
                if (constraints.X) writer.Write(value.x);
                if (constraints.Y) writer.Write(value.y);
                if (constraints.Z) writer.Write(value.z);
            }

            public static void WriteBinary(NetworkWriter writer, Quaternion value, VectorConstraint constraints)
            {
                WriteBinary(writer, value.eulerAngles, constraints);
            }
            #endregion

            #region Read Binary
            public static Vector3 ReadBinary(NetworkReader reader, Vector3 source, VectorConstraint constraints)
            {
                var value = source;

                if (constraints.X) value.x = reader.Read<float>();
                if (constraints.Y) value.y = reader.Read<float>();
                if (constraints.Z) value.z = reader.Read<float>();

                return value;
            }

            public static Quaternion ReadBinary(NetworkReader reader, Quaternion source, VectorConstraint constraints)
            {
                var vector = ReadBinary(reader, source.eulerAngles, constraints);

                return Quaternion.Euler(vector);
            }
            #endregion
        }
        #endregion

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

            public VectorConstraint(bool value) : this(value, value, value) { }
            public VectorConstraint(bool x, bool y, bool z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }

        public Transform Component => transform;

        NetworkWriter writer;
        NetworkReader reader;

        protected override void Reset()
        {
            base.Reset();

#if UNITY_EDITOR
            syncTimer = NetworkEntitySyncTimer.Resolve(Entity);
#endif
        }

        void Awake()
        {
            writer = new NetworkWriter(position.Constraints.BinarySize + rotation.Constraints.BinarySize + scale.Constraints.BinarySize);
            reader = new NetworkReader();

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
            var changed = false;

            for (int i = 0; i < Properties.Count; i++)
                changed |= Properties[i].CheckChanges();

            if (changed || forceSync)
            {
                for (int i = 0; i < Properties.Count; i++)
                    Properties[i].Update(writer);

                var binary = writer.Flush();

                BroadcastRPC(Sync, binary, delivery: DeliveryMode.Unreliable, buffer: RemoteBufferMode.Last, exception: Entity.Owner);
            }
        }

        void Update()
        {
            if (Entity.IsMine == false)
            {
                for (int i = 0; i < Properties.Count; i++)
                    Properties[i].Translate();
            }
        }

        [NetworkRPC]
        void Sync(byte[] binary, RpcInfo info)
        {
            reader.Set(binary);

            for (int i = 0; i < Properties.Count; i++)
                Properties[i].Sync(reader);

            if (info.IsBuffered)
            {
                for (int i = 0; i < Properties.Count; i++)
                    Properties[i].Anchor();
            }
        }
    }
}