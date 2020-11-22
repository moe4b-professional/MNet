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
    public class NetworkAPIConfig : ScriptableObject
    {
        public static NetworkAPIConfig Load()
        {
            var configs = Resources.LoadAll<NetworkAPIConfig>("");

            if (configs.Length == 0) return null;

            var instance = configs[0];

            instance.Configure();

            return instance;
        }

        [SerializeField]
        protected string address = "127.0.0.1";
        public string Address => address;

        [SerializeField]
        protected string appID;
        public string AppID => appID;

        [SerializeField]
        RestScheme restScheme = RestScheme.HTTP;
        public RestScheme RestScheme => restScheme;

        [SerializeField]
        protected VersionProperty version = new VersionProperty("0.0.1");
        public Version Version { get; protected set; }

        void Configure()
        {
            Version = version.Value;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(NetworkAPIConfig))]
        public class Inspector : Editor
        {
            public static GUIStyle VersionLabelStyle;

            void OnEnable()
            {
                VersionLabelStyle = new GUIStyle()
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    //alignment = TextAnchor.MiddleCenter,
                    normal = new GUIStyleState()
                    {
                        textColor = new Color(0.27f, 1, 0.27f),
                    }
                };
            }

            public override void OnInspectorGUI()
            {


                EditorGUILayout.LabelField($"API Version: {Constants.ApiVersion}", VersionLabelStyle);
                EditorGUILayout.Space();
                base.OnInspectorGUI();
            }
        }
#endif
    }

    [Serializable]
    public class VersionProperty
    {
        [SerializeField]
        string value;

        [SerializeField]
        bool infer;

        public string Text => infer ? Application.version : value;

        public Version Value => Version.Parse(Text);

        public VersionProperty(string value)
        {
            this.value = value;
            infer = true;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(VersionProperty))]
        public class Drawer : PropertyDrawer
        {
            SerializedProperty property;
            SerializedProperty value;
            SerializedProperty infer;

            public static GUIContent InferGUIContent;

            void Init(SerializedProperty property)
            {
                if (this.property == property) return;

                this.property = property;

                value = property.FindPropertyRelative(nameof(VersionProperty.value));
                infer = property.FindPropertyRelative(nameof(VersionProperty.infer));

                InferGUIContent = new GUIContent("Infer Version", "Toggle On to Infer Version from the Project's Version");
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

                infer.boolValue = EditorGUI.Toggle(area, InferGUIContent, infer.boolValue);

                rect.y += LineHeight;
                rect.height -= LineHeight;
            }
        }
#endif
    }
}