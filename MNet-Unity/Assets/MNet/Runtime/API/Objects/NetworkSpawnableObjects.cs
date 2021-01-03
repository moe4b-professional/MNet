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
	[CreateAssetMenu(menuName = Constants.Path + "Network Spawnable Objects")]
	public class NetworkSpawnableObjects : ScriptableObject
	{
		[SerializeField]
		List<GameObject> list = default;
		public List<GameObject> List => list;

		public int Count => list.Count;

		public GameObject this[ushort index]
		{
			get
			{
				if (index >= list.Count) return null;

				return list[index];
			}
		}

		public Dictionary<GameObject, ushort> Prefabs { get; protected set; }

		public Dictionary<string, ushort> Names { get; protected set; }

		void OnEnable()
		{
#if UNITY_EDITOR
			Refresh();
#endif

			CheckForDuplicates();

			Prefabs = new Dictionary<GameObject, ushort>();
			Names = new Dictionary<string, ushort>();

			for (ushort i = 0; i < list.Count; i++)
			{
				if (list[i] == null) continue;

				Prefabs.Add(list[i].gameObject, i);
				Names.Add(list[i].name, i);
			}
		}

		void CheckForDuplicates()
		{
			var hash = new HashSet<GameObject>();

			for (int i = 0; i < list.Count; i++)
			{
				if (hash.Contains(list[i]))
					throw new Exception($"Duplicate Network Spawnable Object '{list[i]}' Found at Index {i}");

				hash.Add(list[i]);
			}
		}

		public static NetworkSpawnableObjects Load()
		{
			var assets = Resources.LoadAll<NetworkSpawnableObjects>("");

			if (assets.Length == 0) return null;

			var instance = assets[0];

			return instance;
		}

#if UNITY_EDITOR
		void Refresh()
		{
			var hash = new HashSet<GameObject>(list);

			foreach (var element in GetAll())
			{
				if (hash.Contains(element.gameObject)) continue;

				list.Add(element.gameObject);
			}

			list.RemoveAll(x => x == null);

			EditorUtility.SetDirty(this);
		}

		static IEnumerable<NetworkEntity> GetAll()
		{
			var guids = AssetDatabase.FindAssets($"t:{nameof(GameObject)}");

            for (int i = 0; i < guids.Length; i++)
            {
				var path = AssetDatabase.GUIDToAssetPath(guids[i]);

				var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

				var entity = asset.GetComponent<NetworkEntity>();

				if (entity == null) continue;

				yield return entity;
            }
		}

		[CustomEditor(typeof(NetworkSpawnableObjects))]
		public class Inspector : Editor
        {
			new NetworkSpawnableObjects target;

			void OnEnable()
            {
				target = base.target as NetworkSpawnableObjects;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

				if (GUILayout.Button("Refresh")) target.Refresh();

				EditorGUILayout.HelpBox("These Spawnable Objects Automatically Get Updated Whenever PlayMode is Entered" +
					", So There is no Need to Use These Controls Manually", MessageType.Info);
			}
        }
#endif
	}
}