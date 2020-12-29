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
		bool registered = default;
		public bool Registered
		{
			get
			{
#if UNITY_EDITOR
				if (asset == null) throw new NullReferenceException($"No Scene Asset Defined for GameScene {name}");
#endif

				return registered;
			}
		}

		[SerializeField]
        bool active = default;
        public bool Active
        {
			get
            {
#if UNITY_EDITOR
				if (asset == null) throw new NullReferenceException($"No Scene Asset Defined for GameScene {name}");
#endif

				return active;
			}
        }

        [SerializeField]
		string id = default;
		public string ID
		{
			get
			{
#if UNITY_EDITOR
				if (asset == null) throw new NullReferenceException($"No Scene Asset Defined for GameScene {name}");
#endif

				return id;
			}
		}

		[SerializeField]
		int buildIndex = -1;
		public int BuildIndex
        {
			get
            {
#if UNITY_EDITOR
				if (asset == null) throw new NullReferenceException($"No Scene Asset Defined for GameScene {name}");
#endif

				if (registered == false) throw new Exception($"Scene '{path}' Not Added to Editor Build Settings");

				if (active == false) throw new Exception($"Scene '{path}' Not Active in Editor Build Settings");

				return buildIndex;
			}
        }

		[SerializeField]
		string path = string.Empty;
		public string Path
        {
			get
            {
#if UNITY_EDITOR
				if (asset == null) throw new NullReferenceException($"No Scene Asset Defined for GameScene {name}");
#endif

				return path;
			}
        }

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

			registered = TryGetInfo(asset, out active, out id, out path, out buildIndex);

			EditorUtility.SetDirty(this);
		}

		static bool TryGetInfo(Object asset, out bool active, out string name, out string path, out int buildIndex)
		{
			name = asset.name;
			path = AssetDatabase.GetAssetPath(asset);

			buildIndex = 0;

			foreach (var item in EditorBuildSettings.scenes)
			{
				var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(item.path);

				if (scene == asset)
				{
					active = item.enabled;
					return true;
				}

				if (item.enabled) buildIndex += 1;
			}

			buildIndex = -1;

			active = false;
			return false;
		}
#endif

		public static implicit operator int(GameScene scene) => scene.buildIndex;

		//Static Utility
		public static Dictionary<string, GameScene> Dictionary { get; protected set; }

		public static void Register(GameScene scene)
		{
			Dictionary[scene.ID] = scene;
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