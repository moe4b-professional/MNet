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

namespace MNet.Example
{
    [CreateAssetMenu]
	public class ScriptableObjectInitializer : ScriptableObject
	{
        static ScriptableObjectInitializer _instance;
        public static ScriptableObjectInitializer Instance
        {
            get
            {
                if(_instance == null)
                {
                    var list = Resources.LoadAll<ScriptableObjectInitializer>("");

                    _instance = list.Length == 0 ? null : list[0];
                }

                return _instance;
            }
        }

        [SerializeField]
        protected List<ScriptableObject> targets;
        public IReadOnlyList<ScriptableObject> Targets { get { return targets; } }

        public static void Load()
        {
            if(Instance == null)
            {
                Debug.LogWarning("No " + nameof(ScriptableObjectInitializer) + " instance found, ignoring system load");
                return;
            }

            Instance.Configure();
        }

        protected virtual void Configure()
        {
#if UNITY_EDITOR
            Refresh();
#endif

            IInitialize[] interfaces = new IInitialize[targets.Count];

            for (int i = 0; i < targets.Count; i++)
                interfaces[i] = targets[i] as IInitialize;

            Initializer.Configure(interfaces);

            Initializer.Init(interfaces);
        }

#if UNITY_EDITOR
        public virtual void Refresh()
        {
            targets.Clear();

            var GUIDs = AssetDatabase.FindAssets("t:" + typeof(ScriptableObject));

            for (int i = 0; i < GUIDs.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(GUIDs[i]);

                var instance = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (instance is IInitialize)
                    targets.Add(instance);
            }

            EditorUtility.SetDirty(this);
        }
#endif
    }
}