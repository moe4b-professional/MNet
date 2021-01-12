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
        MasterAddressProperty masterAddress = default;
        public string MasterAddress => masterAddress.Value;
        [Serializable]
        public class MasterAddressProperty
        {
            [SerializeField]
            bool local = false;
            public bool Local => local;

            [SerializeField]
            protected string value = Default;

            public const string Default = "127.0.0.1";

            public string Value => local ? Default : value;

#if UNITY_EDITOR
#if UNITY_EDITOR
            [CustomPropertyDrawer(typeof(MasterAddressProperty))]
            public class Drawer : PropertyDrawer
            {
                SerializedProperty property;
                SerializedProperty local;
                SerializedProperty value;

                public static GUIContent LocalGUIContent;

                void Init(SerializedProperty property)
                {
                    if (this.property == property) return;

                    this.property = property;

                    value = property.FindPropertyRelative(nameof(MasterAddressProperty.value));
                    local = property.FindPropertyRelative(nameof(MasterAddressProperty.local));

                    LocalGUIContent = new GUIContent("Connect to Local", "Toggle On to Always Connect to Localhost, Usefult for Testing");
                }

                public static float LineHeight => EditorGUIUtility.singleLineHeight;

                public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
                {
                    Init(property);

                    return (local.boolValue ? 1 : 2) * LineHeight;
                }

                public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
                {
                    Init(property);

                    DrawLocal(ref position);

                    if (local.boolValue == false) DrawValue(ref position);
                }

                void DrawLocal(ref Rect rect)
                {
                    var area = new Rect(rect.x, rect.y, rect.width, LineHeight);

                    local.boolValue = EditorGUI.Toggle(area, LocalGUIContent, local.boolValue);

                    rect.y += LineHeight;
                    rect.height -= LineHeight;
                }

                void DrawValue(ref Rect rect)
                {
                    var area = new Rect(rect.x, rect.y, rect.width, LineHeight);

                    value.stringValue = EditorGUI.TextField(area, "Master Address", value.stringValue);

                    rect.y += LineHeight;
                    rect.height -= LineHeight;
                }
            }
#endif
#endif
        }

        [SerializeField]
        protected string appID;
        public string AppID => appID;

        [SerializeField]
        RestScheme restScheme = RestScheme.HTTP;
        public RestScheme RestScheme => restScheme;

        [SerializeField]
        protected GameVersionProperty gameVersion = new GameVersionProperty("0.1");
        public Version GameVersion => gameVersion.Value;
        [Serializable]
        public class GameVersionProperty
        {
            [SerializeField]
            bool infer;

            [SerializeField]
            string value;

            public string Text => infer ? Application.version : value;

            public Version Value => Version.Parse(Text);

            public GameVersionProperty(string value)
            {
                this.value = value;
                infer = true;
            }

#if UNITY_EDITOR
            [CustomPropertyDrawer(typeof(GameVersionProperty))]
            public class Drawer : PropertyDrawer
            {
                SerializedProperty property;
                SerializedProperty infer;
                SerializedProperty value;

                public static GUIContent InferGUIContent;

                void Init(SerializedProperty property)
                {
                    if (this.property == property) return;

                    this.property = property;

                    value = property.FindPropertyRelative(nameof(GameVersionProperty.value));
                    infer = property.FindPropertyRelative(nameof(GameVersionProperty.infer));

                    InferGUIContent = new GUIContent("Infer Game Version", "Toggle On to Infer Game Version from the Project's Version");
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

                void DrawInfer(ref Rect rect)
                {
                    var area = new Rect(rect.x, rect.y, rect.width, LineHeight);

                    infer.boolValue = EditorGUI.Toggle(area, InferGUIContent, infer.boolValue);

                    rect.y += LineHeight;
                    rect.height -= LineHeight;
                }

                void DrawValue(ref Rect rect)
                {
                    var area = new Rect(rect.x, rect.y, rect.width, LineHeight);

                    value.stringValue = EditorGUI.TextField(area, "Game Version", value.stringValue);

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