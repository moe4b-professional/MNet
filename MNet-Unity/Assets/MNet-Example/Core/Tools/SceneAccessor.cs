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
	public class SceneAccessor : MonoBehaviour
	{
		public static SceneAccessor Create()
        {
			var gameObject = new GameObject("Scene Accessor");

			DontDestroyOnLoad(gameObject);

			var script = gameObject.AddComponent<SceneAccessor>();

			return script;
        }
	}
}