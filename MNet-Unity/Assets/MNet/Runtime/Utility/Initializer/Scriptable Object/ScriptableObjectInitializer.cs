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
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MNet
{
    [CreateAssetMenu]
	public class ScriptableObjectInitializer : ScriptableObject
	{
        public static ScriptableObjectInitializer Instance { get; protected set; }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void OnLoad()
        {
            var list = Resources.LoadAll<ScriptableObjectInitializer>("");

            if (list.Length == 0)
            {
                Debug.LogWarning($"No {nameof(ScriptableObjectInitializer)} Instance Found, Ignoring System Load");
                return;
            }

            Instance = list[0];

#if UNITY_EDITOR
            Instance.Refresh();
#endif

            Instance.Perform();
        }

        [SerializeField]
        protected List<ScriptableObject> list;
        public IReadOnlyList<ScriptableObject> List { get { return list; } }

        protected virtual void Perform()
        {
            var interfaces = new IInitialize[list.Count];

            for (int i = 0; i < list.Count; i++)
                interfaces[i] = list[i] as IInitialize;

            Initializer.Perform(interfaces);
        }

#if UNITY_EDITOR
        public virtual void Refresh()
        {
            list.Clear();

            var GUIDs = AssetDatabase.FindAssets("t:ScriptableObject");

            for (int i = 0; i < GUIDs.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(GUIDs[i]);

                var instance = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (instance is IInitialize) list.Add(instance);
            }

            EditorUtility.SetDirty(this);
        }
#endif

        public ScriptableObjectInitializer()
        {
            list = new List<ScriptableObject>();
        }

#if UNITY_EDITOR
        class BuildProcessor : IPreprocessBuildWithReport
        {
            public ScriptableObjectInitializer Instance => ScriptableObjectInitializer.Instance;

            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildReport report) => Instance.Refresh();
            public void OnPreprocessBuild(BuildTarget target, string path) => Instance.Refresh();
        }
#endif
    }
}