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

using UnityEditor.SceneManagement;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MNet
{
	[CreateAssetMenu(menuName = Constants.Path + "Network Scene Asset")]
	public class NetworkSceneAsset : ScriptableObject, IInitialize
	{
		[SerializeField]
		Object _asset;
		/// <summary>
		/// EDITOR ONLY, Will return null in Build
		/// </summary>
		public Object Asset
		{
			get => _asset;
			set
			{
				_asset = value;

#if UNITY_EDITOR
				Refresh();
#endif
			}
		}

		[SerializeField]
		bool _registered = default;
		public bool Registered
		{
			get
			{
#if UNITY_EDITOR
				if (Asset == null) return false;
#endif

				return _registered;
			}
		}

		[SerializeField]
        bool _active = default;
        public bool Active
        {
			get
            {
#if UNITY_EDITOR
				if (Asset == null) return false;
#endif

				return _active;
			}
        }

        [SerializeField]
		string _id = default;
		public string ID
		{
			get
			{
#if UNITY_EDITOR
				if (Asset == null) return string.Empty;
#endif

				return _id;
			}
		}

		[SerializeField]
		int _index = default;
		public int Index
        {
			get
            {
#if UNITY_EDITOR
				if (Asset == null) return -1;
#endif

				if (!Registered || !Active) return -1;

				return _index;
			}
        }

		[SerializeField]
		string _path = default;
		public string Path
        {
			get
            {
#if UNITY_EDITOR
				if (Asset == null) return string.Empty;
#endif

				return _path;
			}
        }

		void Reset()
        {
#if UNITY_EDITOR
			Asset = Selection.activeObject as SceneAsset;
#endif
		}

		public virtual void Configure()
		{
#if UNITY_EDITOR
			Refresh();
#endif

			if (Active) Register(this);
		}

		public virtual void Init() { }

#if UNITY_EDITOR
		void Refresh()
		{
			_registered = TryGetInfo(Asset, out _active, out _id, out _path, out _index);

			EditorUtility.SetDirty(this);
		}

		static bool TryGetInfo(Object asset, out bool active, out string name, out string path, out int buildIndex)
		{
			if (asset == null)
			{
				name = string.Empty;
				path = string.Empty;
				buildIndex = -1;
				active = false;
				return false;
			}

			buildIndex = 0;

			foreach (var item in EditorBuildSettings.scenes)
			{
				var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(item.path);

				if (scene == asset)
				{
					name = asset.name;
					path = AssetDatabase.GetAssetPath(asset);
					active = item.enabled;
					return true;
				}

				if (item.enabled) buildIndex += 1;
			}

			name = string.Empty;
			path = string.Empty;
			buildIndex = -1;
			active = false;
			return false;
		}
#endif

        public static implicit operator int(NetworkSceneAsset scene) => scene.Index;

		//Static Utility
		public static Dictionary<string, NetworkSceneAsset> Dictionary { get; protected set; }

        public static void Register(NetworkSceneAsset scene)
		{
			if (Dictionary.ContainsKey(scene.name))
			{
				Debug.LogWarning($"Duplicate Network Scene Asset Registeration for name {scene.name}");
				return;
			}

			Dictionary[scene.ID] = scene;
		}

		public static bool TryFind(string name, out NetworkSceneAsset scene) => Dictionary.TryGetValue(name, out scene);

        static NetworkSceneAsset()
		{
			Dictionary = new Dictionary<string, NetworkSceneAsset>();
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(NetworkSceneAsset))]
		public class Inspector : Editor
		{
			new NetworkSceneAsset target;

			void OnEnable()
			{
				target = base.target as NetworkSceneAsset;
			}

			public override void OnInspectorGUI()
			{
				target.Asset = EditorGUILayout.ObjectField("Scene", target.Asset, typeof(SceneAsset), false);
			}
		}
#endif
	}
}