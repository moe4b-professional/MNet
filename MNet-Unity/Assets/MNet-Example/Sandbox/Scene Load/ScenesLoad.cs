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

using Cysharp.Threading.Tasks;

namespace MNet
{
	public class ScenesLoad : NetworkBehaviour
	{
		public GameScene[] scenes = default;

		public LoadSceneMode mode = LoadSceneMode.Single;

		void Request()
        {
			NetworkAPI.Room.Scenes.Load(mode, scenes);
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(ScenesLoad))]
		class Inspector : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

				EditorGUILayout.Space();

				GUI.enabled = Application.isPlaying;

				if(GUILayout.Button("Load"))
                {
					var target = base.target as ScenesLoad;

					target.Request();
                }
            }
        }
#endif
    }
}