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

using UnityEditor.SceneManagement;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MNet
{
	[CreateAssetMenu]
	public class GameScene : ScriptableObject
	{
		[SerializeField]
		Object asset;
		/// <summary>
		/// EDITOR ONLY, Will return null in Build
		/// </summary>
		public Object Asset
		{
			get => asset;
			set
			{
				asset = value;

#if UNITY_EDITOR
				Refresh();
#endif
			}
		}

		[SerializeField]
		string _name = default;
		new public string name
		{
			get => _name;
			set => _name = value;
		}

		[SerializeField]
		int buildIndex = -1;
		public int BuildIndex => buildIndex;

		[SerializeField]
		string path = string.Empty;
		public string Path => path;

		void OnEnable()
		{
#if UNITY_EDITOR
			Refresh();
#endif

			Register(this);
		}

#if UNITY_EDITOR
		void Refresh()
		{
			if (asset == null)
			{
				buildIndex = -1;
				path = string.Empty;
				return;
			}

			var list = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);

			for (int i = 0; i < list.Length; i++)
			{
				var instance = AssetDatabase.LoadAssetAtPath<SceneAsset>(list[i]);

				if (asset != instance) continue;

				buildIndex = i;
				path = list[i];

				name = System.IO.Path.GetFileNameWithoutExtension(Path);

				break;
			}

			EditorUtility.SetDirty(this);
		}
#endif

		public static implicit operator int(GameScene scene) => scene.buildIndex;

		//Static Utility
		public static Dictionary<string, GameScene> Dictionary { get; protected set; }

		public static void Register(GameScene scene)
		{
			Dictionary[scene.name] = scene;
		}

		public static bool TryFind(string name, out GameScene scene) => Dictionary.TryGetValue(name, out scene);

		static GameScene()
		{
			Dictionary = new Dictionary<string, GameScene>();
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(GameScene))]
		public class Inspector : Editor
		{
			new GameScene target;

			void OnEnable()
			{
				target = base.target as GameScene;
			}

			public override void OnInspectorGUI()
			{
				target.Asset = EditorGUILayout.ObjectField("Scene", target.asset, typeof(SceneAsset), false);
			}
		}
#endif
	}
}