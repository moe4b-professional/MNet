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
using MNet;

namespace MNet
{
    [CreateAssetMenu]
    public class MNetAPIConfig : ScriptableObject
    {
        public static MNetAPIConfig Load()
        {
            var configs = Resources.LoadAll<MNetAPIConfig>("");

            if (configs.Length == 0) return null;

            var instance = configs[0];

            instance.Configure();

            return instance;
        }

        [SerializeField]
        protected string address = "127.0.0.1";
        public string Address => address;

        [SerializeField]
        RestScheme restScheme = RestScheme.HTTP;
        public RestScheme RestScheme => restScheme;

        [SerializeField]
        protected NetworkTransportType transport;
        public NetworkTransportType Transport => transport;

        [SerializeField]
        protected NetworkVersionProperty version = new NetworkVersionProperty("0.0.1");
        public Version Version { get; protected set; }

        void Configure()
        {
            Version = version.Value;
        }
    }

    [Serializable]
    public class NetworkVersionProperty
    {
        [SerializeField]
        string value;

        [SerializeField]
        bool infer;

        public string Text => infer ? Application.version : value;

        public Version Value => Version.Parse(Text);

        public NetworkVersionProperty(string value)
        {
            this.value = value;
            infer = true;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(NetworkVersionProperty))]
        public class Drawer : PropertyDrawer
        {
            SerializedProperty property;
            SerializedProperty value;
            SerializedProperty infer;

            void Init(SerializedProperty property)
            {
                this.property = property;

                value = property.FindPropertyRelative(nameof(NetworkVersionProperty.value));
                infer = property.FindPropertyRelative(nameof(NetworkVersionProperty.infer));
            }

            public static float LineHeight => EditorGUIUtility.singleLineHeight;

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                Init(property);

                return (infer.boolValue ? 1 : 2) * LineHeight;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                Init(property);

                DrawInfer(ref position);

                if (infer.boolValue == false) DrawValue(ref position);
            }

            void DrawValue(ref Rect rect)
            {
                var area = new Rect(rect.x, rect.y, rect.width, LineHeight);

                value.stringValue = EditorGUI.TextField(area, "Version", value.stringValue);

                rect.y += LineHeight;
                rect.height -= LineHeight;
            }

            void DrawInfer(ref Rect rect)
            {
                var area = new Rect(rect.x, rect.y, rect.width, LineHeight);

                infer.boolValue = EditorGUI.Toggle(area, "Infer Version", infer.boolValue);

                rect.y += LineHeight;
                rect.height -= LineHeight;
            }
        }
#endif
    }
}