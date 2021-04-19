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
	public class ScenesUnload : MonoBehaviour
	{
		public GameScene scene = default;

		void Request()
		{
			NetworkAPI.Room.Scenes.Unload.Request(scene);
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(ScenesUnload))]
		class Inspector : Editor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				EditorGUILayout.Space();

				GUI.enabled = Application.isPlaying;

				if (GUILayout.Button("Unload"))
				{
					var target = base.target as ScenesUnload;

					target.Request();
				}
			}
		}
#endif
	}
}