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
    public class NetworkAPIConfig : ScriptableObject, IInitialize, IScriptableObjectBuildPreProcess
    {
        [SerializeField]
        RestScheme restScheme = RestScheme.HTTP;
        public RestScheme RestScheme => restScheme;

        public AppID AppID => appID.Selection;
        public Version GameVersion => gameVersion.Selection;
        public string MasterAddress => masterAddress.Selection;

        [SerializeField]
        AppIDsProperty appID = new AppIDsProperty();
        [Serializable]
        public class AppIDsProperty : Property
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

            public AppID Selection { get; protected set; }

            public override void Configure()
            {
                var platform = MUtility.CheckPlatform();

                Selection = Retrieve(platform);
            }

            public AppID Retrieve(RuntimePlatform platform)
            {
                for (int i = 0; i < overrides.Length; i++)
                    if (overrides[i].Platforms.Contains(platform))
                        return new AppID(overrides[i].ID);

                return new AppID(global);
            }
        }

        [SerializeField]
        MasterAddressProperty masterAddress = new MasterAddressProperty();
        [Serializable]
        public class MasterAddressProperty : Property
        {
            [SerializeField]
            bool local = false;
            public bool Local => local;

            public const string Default = "127.0.0.1";

            [SerializeField]
            protected string value = Default;

            public string Selection => local ? Default : value;

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

                    LocalGUIContent = new GUIContent("Connect to Local", "Toggle On to Always Connect to Localhost, usefull for Testing");
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
        protected GameVersionProperty gameVersion = new GameVersionProperty();
        [Serializable]
        public class GameVersionProperty : Property
        {
            [SerializeField]
            bool infer = true;
            public bool Infer => infer;

            [SerializeField]
            Version value = new Version(0, 1, 0);

            public Version Selection { get; protected set; }

            public override void Configure()
            {
                base.Configure();

                if (infer)
                {
                    if (Version.TryParse(Application.version, out var version) == false)
                    {
                        throw new Exception($"Game Version Cannot be Infered from '{Application.version}', " +
                        $"Please Modify your Project Settings to have a valid Version in the Format of x.x.x");
                    }

                    Selection = version;
                }
                else
                {
                    Selection = value;
                }
            }

#if UNITY_EDITOR
            [CustomPropertyDrawer(typeof(GameVersionProperty))]
            public class Drawer : PropertyDrawer
            {
                SerializedProperty property;
                SerializedProperty infer;

                SerializedProperty value;

                public static GUIContent InferGUIContent;

                public static float LineHeight => EditorGUIUtility.singleLineHeight;

                void Init(SerializedProperty reference)
                {
                    if (property?.propertyPath == reference?.propertyPath) return;

                    property = reference;

                    infer = reference.FindPropertyRelative(nameof(infer));
                    value = reference.FindPropertyRelative(nameof(value));

                    InferGUIContent = new GUIContent("Infer Game Version", "Toggle On to Infer Game Version from the Project's Version");
                }

                public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => LineHeight * 2;

                public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
                {
                    Init(property);

                    DrawValue(ref rect, label);
                    DrawInfer(ref rect);
                }

                void DrawValue(ref Rect rect, GUIContent label)
                {
                    var area = new Rect(rect.x, rect.y, rect.width, LineHeight);

                    rect.y += LineHeight;

                    if (infer.boolValue)
                    {
                        if (Version.TryParse(Application.version, out var version) == false)
                        {
                            EditorGUI.HelpBox(area, $"Game Version Cannot be Infered from '{Application.version}'", MessageType.Error);
                        }
                        else
                        {
                            GUI.enabled = false;
                            Version.Drawer.DrawReadOnly(area, label, version);
                            GUI.enabled = true;
                        }
                    }
                    else
                    {
                        EditorGUI.PropertyField(area, value, label);
                    }
                }

                void DrawInfer(ref Rect rect)
                {
                    var area = new Rect(rect.x, rect.y, rect.width, LineHeight);

                    infer.boolValue = EditorGUI.Toggle(area, InferGUIContent, infer.boolValue);
                }
            }
#endif
        }

        [SerializeField]
        UpdateMethodProperty updateMethod = new UpdateMethodProperty();
        public UpdateMethodProperty UpdateMethod => updateMethod;
        [Serializable]
        public class UpdateMethodProperty : Property
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
        public class SyncedAssetsProperty : Property
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

            public override void Configure()
            {
                base.Configure();

#if UNITY_EDITOR
                if (autoUpdate) Refresh();
#endif

                Indexes = new Dictionary<Object, ushort>();

                for (ushort i = 0; i < list.Count; i++)
                    Indexes.Add(list[i], i);
            }

            public SyncedAssetsProperty()
            {
                list = new List<Object>();
            }

#if UNITY_EDITOR
            internal void Refresh()
            {
                var set = new HashSet<Object>(list);

                set.RemoveWhere(x => CheckAsset(x) == false);

                foreach (var element in Iterate(set))
                    set.Add(element);

                if (MUtility.CheckElementsInclusion(set, list) == false)
                {
                    list = set.ToList();
                    EditorUtility.SetDirty(Config);
                }
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
                if (asset == null) return false;

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

        public class Property : IInitialize, IReference<NetworkAPIConfig>
        {
            public NetworkAPIConfig Config { get; protected set; }
            public virtual void Set(NetworkAPIConfig context) => Config = context;

            public virtual void Configure() { }

            public virtual void Init() { }
        }
        public IEnumerable<Property> AllProperties()
        {
            yield return appID;
            yield return masterAddress;
            yield return gameVersion;
            yield return updateMethod;
            yield return syncedAssets;
        }

        public static NetworkAPIConfig Load()
        {
            var assets = Resources.LoadAll<NetworkAPIConfig>("");

            if (assets.Length == 0)
            {
                Debug.LogWarning("No Network API Config Asset Found");
                return null;
            }

            var instance = assets[0];

            return instance;
        }

        public void Configure()
        {
            References.Set(this, AllProperties);

            Initializer.Configure(AllProperties);
        }

        public void Init()
        {
            Initializer.Init(AllProperties);
        }

#if UNITY_EDITOR
        public void PreProcessBuild()
        {
            syncedAssets.Refresh();
        }

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