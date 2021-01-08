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
    [CreateAssetMenu(menuName = Constants.Path + "Network API Config")]
    public class NetworkAPIConfig : ScriptableObject
    {
        [SerializeField]
        protected string address = "127.0.0.1";
        public string Address => connectToLocal ? "127.0.0.1" : address;

        [SerializeField]
        bool connectToLocal = default;

        [SerializeField]
        protected string appID;
        public string AppID => appID;

        [SerializeField]
        RestScheme restScheme = RestScheme.HTTP;
        public RestScheme RestScheme => restScheme;

        [SerializeField]
        protected VersionProperty version = new VersionProperty("0.1");
        public Version Version => version.Value;
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

        [SerializeField]
        UpdateMethodProperty updateMethod = default;
        public UpdateMethodProperty UpdateMethod => updateMethod;
        [Serializable]
        public class UpdateMethodProperty
        {
            [SerializeField]
            bool early = false;
            public bool Early => early;

            [SerializeField]
            bool normal = true;
            public bool Normal => normal;

            [SerializeField]
            bool @fixed = false;
            public bool Fixed => @fixed;

            [SerializeField]
            LateProperty late = new LateProperty();
            public LateProperty Late => late;
            [Serializable]
            public class LateProperty
            {
                [SerializeField]
                bool pre = true;
                public bool Pre => pre;

                [SerializeField]
                bool post = false;
                public bool Post => post;
            }

            public bool Any => early || normal || @fixed || late.Pre || late.Post;
        }

        public static NetworkAPIConfig Load()
        {
            var assets = Resources.LoadAll<NetworkAPIConfig>("");

            if (assets.Length == 0) return null;

            var instance = assets[0];

            return instance;
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
}