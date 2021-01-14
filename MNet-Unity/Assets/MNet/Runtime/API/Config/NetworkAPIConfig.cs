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
        MasterAddressProperty masterAddress = new MasterAddressProperty();
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

                public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => LineHeight * 2;

                public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
                {
                    Init(property);

                    DrawValue(ref position);
                    DrawLocal(ref position);
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
                    if (local.boolValue) GUI.enabled = false;

                    var area = new Rect(rect.x, rect.y, rect.width, LineHeight);

                    if (local.boolValue)
                        EditorGUI.TextField(area, "Master Address", Default);
                    else
                        value.stringValue = EditorGUI.TextField(area, "Master Address", value.stringValue);

                    rect.y += LineHeight;
                    rect.height -= LineHeight;

                    GUI.enabled = true;
                }
            }
#endif
        }

        [SerializeField]
        protected string appID = "AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"; //AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHHHHHHHHHHHHH
        public AppID AppID { get; protected set; }

        [SerializeField]
        RestScheme restScheme = RestScheme.HTTP;
        public RestScheme RestScheme => restScheme;

        [SerializeField]
        protected GameVersionProperty gameVersion = new GameVersionProperty();
        public Version GameVersion { get; protected set; }
        [Serializable]
        public class GameVersionProperty
        {
            [SerializeField]
            bool infer;
            public bool Infer => infer;

            [SerializeField]
            byte major;

            [SerializeField]
            byte minor;

            [SerializeField]
            byte patch;

            public Version Value => infer ? Version.Parse(Application.version) : new Version(major, minor, patch);

            public GameVersionProperty()
            {
                major = 0;
                minor = 1;
                patch = 0;

                infer = true;
            }

#if UNITY_EDITOR
            [CustomPropertyDrawer(typeof(GameVersionProperty))]
            public class Drawer : PropertyDrawer
            {
                SerializedProperty property;
                SerializedProperty infer;

                SerializedProperty major;
                SerializedProperty minor;
                SerializedProperty patch;

                public static GUIContent InferGUIContent;

                public const int FieldWidth = 30;

                public const int SeperatorWidth = 8;
                public static GUIStyle SeperatorStyle;

                void Init(SerializedProperty property)
                {
                    if (this.property == property) return;

                    this.property = property;

                    infer = property.FindPropertyRelative(nameof(GameVersionProperty.infer));

                    major = property.FindPropertyRelative(nameof(GameVersionProperty.major));
                    minor = property.FindPropertyRelative(nameof(GameVersionProperty.minor));
                    patch = property.FindPropertyRelative(nameof(GameVersionProperty.patch));

                    InferGUIContent = new GUIContent("Infer Game Version", "Toggle On to Infer Game Version from the Project's Version");

                    SeperatorStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 20,
                        fontStyle = FontStyle.Bold,
                    };
                }

                public static float LineHeight => EditorGUIUtility.singleLineHeight;

                public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => LineHeight * 2;

                public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
                {
                    Init(property);

                    DrawValue(ref position);
                    DrawInfer(ref position);
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
                    var x = rect.x;
                    var width = rect.width;

                    DrawLabel(ref rect);

                    if(infer.boolValue)
                    {
                        if (Version.TryParse(Application.version, out var version) == false)
                        {
                            var area = new Rect(rect.x, rect.y, rect.width, LineHeight);

                            EditorGUI.HelpBox(area, $"Game Version Cannot be Infered from '{Application.version}'", MessageType.Error);
                        }
                        else
                        {
                            GUI.enabled = false;

                            DrawField(ref rect, version.Major);
                            DrawSeperator(ref rect);
                            DrawField(ref rect, version.Minor);
                            DrawSeperator(ref rect);
                            DrawField(ref rect, version.Patch);

                            GUI.enabled = true;
                        }
                    }
                    else
                    {
                        DrawField(ref rect, major);
                        DrawSeperator(ref rect);
                        DrawField(ref rect, minor);
                        DrawSeperator(ref rect);
                        DrawField(ref rect, patch);
                    }

                    rect.x = x;
                    rect.width = width;
                    rect.y += LineHeight;
                    rect.height -= LineHeight;
                }

                void DrawLabel(ref Rect rect)
                {
                    var width = EditorGUIUtility.labelWidth;

                    var area = new Rect(rect.x, rect.y, width, LineHeight);

                    EditorGUI.LabelField(area, "Game Version");

                    rect.width -= width;
                    rect.x += width;
                }

                void DrawField(ref Rect rect, SerializedProperty property)
                {
                    var area = new Rect(rect.x, rect.y, FieldWidth, LineHeight);

                    property.intValue = EditorGUI.IntField(area, property.intValue);

                    rect.x += FieldWidth;
                    rect.width -= FieldWidth;
                }

                void DrawField(ref Rect rect, byte value)
                {
                    var area = new Rect(rect.x, rect.y, FieldWidth, LineHeight);

                    EditorGUI.IntField(area, value);

                    rect.x += FieldWidth;
                    rect.width -= FieldWidth;
                }

                void DrawSeperator(ref Rect rect)
                {
                    var area = new Rect(rect.x, rect.y, SeperatorWidth, LineHeight);

                    EditorGUI.LabelField(area, ".", SeperatorStyle);

                    rect.x += SeperatorWidth;
                    rect.width -= SeperatorWidth;
                }
            }
#endif
        }

        [SerializeField]
        UpdateMethodProperty updateMethod = new UpdateMethodProperty();
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

        [SerializeField]
        SpawnableObjectsProperty spawnableObjects = new SpawnableObjectsProperty();
        public SpawnableObjectsProperty SpawnableObjects => spawnableObjects;
        [Serializable]
        public class SpawnableObjectsProperty
        {
            [SerializeField]
            bool autoUpdate = true;
            public bool AutoUpdate => autoUpdate;

            [SerializeField]
            List<GameObject> list;
            public List<GameObject> List => list;

            public int Count => list.Count;

            public GameObject this[ushort index]
            {
                get
                {
                    if (index >= list.Count) return null;

                    return list[index];
                }
            }

            public Dictionary<GameObject, ushort> Prefabs { get; protected set; }
            public bool TryGetIndex(GameObject prefab, out ushort index) => Prefabs.TryGetValue(prefab, out index);

            public Dictionary<string, ushort> Names { get; protected set; }
            public bool TryGetIndex(string name, out ushort index) => Names.TryGetValue(name, out index);

            NetworkAPIConfig config;

            internal void Configure(NetworkAPIConfig reference)
            {
                config = reference;

#if UNITY_EDITOR
                if (autoUpdate) Refresh();
#endif

                CheckForDuplicates();

                Prefabs = new Dictionary<GameObject, ushort>();
                Names = new Dictionary<string, ushort>();

                for (ushort i = 0; i < list.Count; i++)
                {
                    if (list[i] == null) continue;

                    Prefabs.Add(list[i].gameObject, i);
                    Names.Add(list[i].name, i);
                }
            }

            void CheckForDuplicates()
            {
                var hash = new HashSet<GameObject>();

                for (int i = 0; i < list.Count; i++)
                {
                    if (hash.Contains(list[i]))
                        throw new Exception($"Duplicate Network Spawnable Object '{list[i]}' Found at Index {i}");

                    hash.Add(list[i]);
                }
            }

            public SpawnableObjectsProperty()
            {
                list = new List<GameObject>();
            }

#if UNITY_EDITOR
            void Refresh()
            {
                var hash = new HashSet<GameObject>(list);

                foreach (var element in GetAll())
                {
                    if (hash.Contains(element.gameObject)) continue;

                    list.Add(element.gameObject);
                }

                list.RemoveAll(x => x == null);

                EditorUtility.SetDirty(config);
            }

            static IEnumerable<NetworkEntity> GetAll()
            {
                var guids = AssetDatabase.FindAssets($"t:{nameof(GameObject)}");

                for (int i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);

                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    var entity = asset.GetComponent<NetworkEntity>();

                    if (entity == null) continue;

                    yield return entity;
                }
            }
#endif
        }

        public static NetworkAPIConfig Load()
        {
            var assets = Resources.LoadAll<NetworkAPIConfig>("");

            if (assets.Length == 0) return null;

            var instance = assets[0];

            return instance;
        }

        void OnEnable()
        {
            ParseAppID();

            ParseGameVersion();

            spawnableObjects.Configure(this);
        }

        void ParseAppID()
        {
            try
            {
                AppID = AppID.Parse(appID);
            }
            catch (Exception)
            {
                throw new Exception($"Couldn't Parse '{appID}' as App ID, Please Enter a Valid App ID in Network API Config");
            }
        }

        void ParseGameVersion()
        {
            try
            {
                GameVersion = gameVersion.Value;
            }
            catch (Exception)
            {
                if (gameVersion.Infer)
                    throw new Exception($"Game Version Cannot be Infered from '{Application.version}', " +
                        $"Please Modify your Project Settings to have a valid Version in the Format of x.x.x");
                else
                    throw new Exception($"Game Version Cannot be Parsed from Current Network API Config, " +
                        $"Please Modify your Config to have a valid Version in the Format of x.x.x");
            }
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