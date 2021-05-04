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
	public class Initializer : MonoBehaviour
	{
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnLoad()
        {
            SceneManager.sceneLoaded += SceneLoadCallback;
        }

        static void SceneLoadCallback(Scene scene, LoadSceneMode mode)
        {
            var roots = scene.GetRootGameObjects();

            var targets = new List<IInitialize>();

            for (int i = 0; i < roots.Length; i++)
            {
                var range = roots[i].GetComponentsInChildren<IInitialize>(true);

                targets.AddRange(range);
            }

            Perform(targets);
        }

        public static IList<IInitialize> Perform(Component component) => Perform(component.gameObject);
        public static IList<IInitialize> Perform(GameObject gameObject)
        {
            var targets = gameObject.GetComponentsInChildren<IInitialize>(true);

            Perform(targets);

            return targets;
        }
        public static void Perform(IList<IInitialize> list)
        {
            Configure(list);
            Init(list);
        }

        public static IList<IInitialize> Configure(Component component) => Configure(component.gameObject);
        public static IList<IInitialize> Configure(GameObject gameObject)
        {
            var targets = gameObject.GetComponentsInChildren<IInitialize>(true);

            Configure(targets);

            return targets;
        }
        public static void Configure(IList<IInitialize> list)
        {
            for (int i = 0; i < list.Count; i++)
                Configure(list[i]);
        }
        public static void Configure(IInitialize instance)
        {
            instance.Configure();
        }

        public static IList<IInitialize> Init(Component component) => Init(component.gameObject);
        public static IList<IInitialize> Init(GameObject gameObject)
        {
            var targets = gameObject.GetComponentsInChildren<IInitialize>(true);

            Init(targets);

            return targets;
        }
        public static void Init(IList<IInitialize> list)
        {
            for (int i = 0; i < list.Count; i++)
                Init(list[i]);
        }
        public static void Init(IInitialize instance)
        {
            instance.Init();
        }
    }

    public interface IInitialize
    {
        void Configure();

        void Init();
    }
}