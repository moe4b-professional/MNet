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
        [Tooltip("Sync Interval in ms, 1s = 1000ms")]
        SyncInvervalProperty syncInverval = new SyncInvervalProperty(100);
        public SyncInvervalProperty SyncInterval => syncInverval;

        [SerializeField]
        VectorCoordinateProperty position = new VectorCoordinateProperty(true);
        public VectorCoordinateProperty Position => position;
        
        [SerializeField]
        QuaternionCoordinateProperty rotation = new QuaternionCoordinateProperty();
        public QuaternionCoordinateProperty Rotation => rotation;

        [SerializeField]
        VectorCoordinateProperty scale = new VectorCoordinateProperty(false);
        public VectorCoordinateProperty Scale => scale;

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

            position.Set(transform.position);
            rotation.Set(transform.rotation);
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
                if (IsMine)
                    yield return LocalProcedure();
                else
                    yield return RemoteProcedure();
            }
        }

        YieldInstruction LocalProcedure()
        {
            if (IsConnected)
            {
                bool updated = false;

                updated |= position.Update(transform.position);
                updated |= rotation.Update(transform.rotation);
                updated |= scale.Update(transform.localScale);

                if (updated) Broadcast();
            }

            return new WaitForSeconds(syncInverval.Seconds);
        }

        YieldInstruction RemoteProcedure()
        {
            if (position.Target.Any) transform.position = position.MoveTowards(transform.position);
            if (rotation.Target.Any) transform.rotation = rotation.MoveTowards(transform.rotation);
            if (scale.Target.Any) transform.localScale = scale.MoveTowards(transform.localScale);

            return new WaitForEndOfFrame();
        }

        void Broadcast()
        {
            position.WriteBinary(writer);
            rotation.WriteBinary(writer);
            scale.WriteBinary(writer);

            var raw = writer.Flush();

            BroadcastRPC(Sync, raw, buffer: RpcBufferMode.Last, exception: Owner.ID);
        }

        [NetworkRPC(Authority = RemoteAuthority.Owner, Delivery = DeliveryMode.Unreliable)]
        void Sync(byte[] binary, RpcInfo info)
        {
            reader.Set(binary);

            position.ReadBinary(reader);
            rotation.ReadBinary(reader);
            scale.ReadBinary(reader);

            if(info.IsBuffered)
            {
                transform.position = position.Value;
                transform.rotation = rotation.Value;
                transform.localScale = scale.Value;
            }
        }
    }

    [Serializable]
    public struct SyncInvervalProperty
    {
        [SerializeField]
        int value;
        public int Value => value;

        public float Seconds => value / 1000f;

        public SyncInvervalProperty(int value)
        {
            this.value = value;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SyncInvervalProperty))]
        public class Drawer : PropertyDrawer
        {
            public const float Spacing = 80;

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                var value = property.FindPropertyRelative(nameof(SyncInvervalProperty.value));

                DrawValue(ref rect, label, value);

                DrawFrames(ref rect, value.intValue);
            }

            void DrawValue(ref Rect rect, GUIContent label, SerializedProperty value)
            {
                var area = rect;

                area.width -= Spacing;

                value.intValue = EditorGUI.IntSlider(area, label, value.intValue, 0, 200);

                rect.x += area.width;
                rect.width -= area.width;
            }

            void DrawFrames(ref Rect rect, int value)
            {
                var area = rect;

                string symbol = value == 0 ? "∞" : $"~{1000 / value}";
                var text = $"{symbol}Hz";

                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                EditorGUI.LabelField(area, text, style);
            }
        }
#endif
    }
}