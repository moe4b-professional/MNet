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
	public class Initializer : MonoBehaviour
	{
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnLoad()
        {
            SceneManager.sceneLoaded += SceneLoadCallback;

            ScriptableObjectInitializer.Load();
        }

        static void SceneLoadCallback(Scene scene, LoadSceneMode mode)
        {
            var roots = scene.GetRootGameObjects();

            var targets = new List<IInitialize>();

            for (int i = 0; i < roots.Length; i++)
                targets.AddRange(Dependancy.GetAll<IInitialize>(roots[i]));

            Perform(targets);
        }

        public static void Perform(Component component) => Perform(component.gameObject);
        public static void Perform(GameObject gameObject)
        {
            var targets = Dependancy.GetAll<IInitialize>(gameObject);

            Perform(targets);
        }
        public static void Perform(IList<IInitialize> list)
        {
            Configure(list);

            Init(list);
        }

        public static void Configure(Component component) => Configure(component.gameObject);
        public static void Configure(GameObject gameObject)
        {
            var targets = Dependancy.GetAll<IInitialize>(gameObject);

            Configure(targets);
        }
        public static void Configure(IList<IInitialize> list)
        {
            for (int i = 0; i < list.Count; i++)
                Configure(list[i]);
        }
        public static void Configure(IInitialize instance)
        {
            if (instance == null) return;

            instance.Configure();
        }

        public static void Init(Component component) => Init(component.gameObject);
        public static void Init(GameObject gameObject)
        {
            var targets = Dependancy.GetAll<IInitialize>(gameObject);

            Init(targets);
        }
        public static void Init(IList<IInitialize> list)
        {
            for (int i = 0; i < list.Count; i++)
                Init(list[i]);
        }
        public static void Init(IInitialize instance)
        {
            if (instance == null) return;

            instance.Init();
        }
    }

    public interface IInitialize
    {
        void Configure();

        void Init();
    }
}