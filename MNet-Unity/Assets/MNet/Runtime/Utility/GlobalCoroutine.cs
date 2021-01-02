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
	public class GlobalCoroutine : MonoBehaviour
	{
		static GlobalCoroutine instance;

		public static void Configure()
		{
			var gameObject = new GameObject("Global Coroutine");

			instance = gameObject.AddComponent<GlobalCoroutine>();

			DontDestroyOnLoad(instance);
		}

		public static Coroutine Start(Func<IEnumerator> function) => Start(function());
		public static Coroutine Start(IEnumerator ienumerator) => instance.StartCoroutine(ienumerator);

		public static void Stop(Coroutine coroutine) => instance.StopCoroutine(coroutine);

		public static void StopAll() => instance.StopAllCoroutines();
	}
}