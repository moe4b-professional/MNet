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

using MB;
using UnityEngine.UIElements;

namespace MNet
{
    [CreateAssetMenu(menuName = Constants.Path + "Network API Config")]
    public class NetworkAPIConfig : ScriptableObject
    {
        [SerializeField]
        AppIDsProperty appID = new AppIDsProperty();
        [Serializable]
        public class AppIDsProperty
        {
            [SerializeField]
            string global = "Game 1 (Global)";
            public string Global => global;

            [SerializeField]
            Element[] overrides = { new Element("Game 1 (Web)", RuntimePlatform.WebGLPlayer) };
            public Element[] Overrides => overrides;
            [Serializable]
            public class Element
            {
                [SerializeField]
                string _ID = default;
                public string ID => _ID;

                [SerializeField]
                RuntimePlatform[] platforms = default;
                public RuntimePlatform[] Platforms => platforms;

                public Element(string id, params RuntimePlatform[] platforms)
                {
                    this._ID = id;
                    this.platforms = platforms;
                }
            }

            public AppID Retrieve(RuntimePlatform platform)
            {
                for (int i = 0; i < overrides.Length; i++)
                    if (overrides[i].Platforms.Contains(platform))
                        return new AppID(overrides[i].ID);

                return new AppID(global);
            }
        }

        public AppID AppID { get; internal set; }

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

                    if (infer.boolValue)
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
        SyncedAssetsProperty syncedAssets = new SyncedAssetsProperty();
        public SyncedAssetsProperty SyncedAssets => syncedAssets;
        [Serializable]
        public class SyncedAssetsProperty
        {
            [SerializeField]
            bool autoUpdate = true;
            public bool AutoUpdate => autoUpdate;

            [SerializeField]
            List<Object> list;
            public List<Object> List => list;

            public int Count => list.Count;

            public Object this[ushort index]
            {
                get
                {
                    if (index >= list.Count) return null;

                    return list[index];
                }
            }

            public Dictionary<Object, ushort> Indexes { get; protected set; }
            public bool TryGetIndex(Object prefab, out ushort index) => Indexes.TryGetValue(prefab, out index);

            NetworkAPIConfig config;

            internal void Configure(NetworkAPIConfig reference)
            {
                config = reference;

#if UNITY_EDITOR
                if (autoUpdate) Refresh();
#endif

                CheckForDuplicates();

                Indexes = new Dictionary<Object, ushort>();

                for (ushort i = 0; i < list.Count; i++)
                {
                    if (list[i] == null) continue;

                    Indexes.Add(list[i], i);
                }
            }

            void CheckForDuplicates()
            {
                var hash = new HashSet<object>();

                for (int i = 0; i < list.Count; i++)
                {
                    if (hash.Contains(list[i]))
                        throw new Exception($"Duplicate Network Synced Asset '{list[i]}' Found at Index {i}");

                    hash.Add(list[i]);
                }
            }

            public SyncedAssetsProperty()
            {
                list = new List<Object>();
            }

#if UNITY_EDITOR
            void Refresh()
            {
                var hash = new HashSet<Object>(list);

                foreach (var element in Iterate(hash))
                    list.Add(element);

                list.RemoveAll(x => x == null || CheckAsset(x) == false);

                EditorUtility.SetDirty(config);
            }

            static IEnumerable<Object> Iterate(HashSet<Object> exceptions)
            {
                var paths = AssetDatabase.GetAllAssetPaths();

                for (int i = 0; i < paths.Length; i++)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(paths[i]);

                    if (exceptions.Contains(asset)) continue;

                    if (CheckAsset(asset) == false) continue;

                    yield return asset;
                }
            }

            static bool CheckAsset(Object asset)
            {
                switch (asset)
                {
                    case GameObject gameObject:
                        return NetworkEntity.IsAttachedTo(gameObject);

                    case ScriptableObject scriptableObject:
                        return scriptableObject is ISyncedAsset;
                }

                return false;
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
            SelectAppID();

            ParseGameVersion();

            syncedAssets.Configure(this);
        }

        void SelectAppID()
        {
            var platform = MUtility.CheckPlatform();

            AppID = appID.Retrieve(platform);
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

        public class Settings : SettingsProvider
        {
            NetworkAPIConfig config;
            SerializedObject serializedObject;

            public override void OnActivate(string searchContext, VisualElement rootElement)
            {
                base.OnActivate(searchContext, rootElement);

                var config = NetworkAPIConfig.Load();

                serializedObject = new SerializedObject(config);
            }

            public override void OnGUI(string searchContext)
            {
                DrawProperies(serializedObject);

                serializedObject.ApplyModifiedProperties();
            }

            void DrawProperies(SerializedObject root)
            {
                var iterator = root.GetIterator();

                iterator.NextVisible(true);

                while (true)
                {
                    if (iterator.name.StartsWith("m_") == false) DrawProperty(iterator);

                    if (iterator.NextVisible(false) == false) break;
                }
            }

            void DrawProperty(SerializedProperty property) => EditorGUILayout.PropertyField(property, true);

            public Settings(string path, SettingsScope scope) : base(path, scope)
            {

            }

            [SettingsProvider]
            public static SettingsProvider Register()
            {
                var instance = new Settings("Project/MNet Configuration", SettingsScope.Project);

                return instance;
            }
        }
#endif
    }
}