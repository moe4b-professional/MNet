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
	public class NetworkEntityStresser : NetworkBehaviour
	{
		public PrefabAsset prefab;

		public int count = 2000;

		public int area = 50;

		void Perform()
        {
			StartCoroutine(Coroutine());
			IEnumerator Coroutine()
			{
				for (int i = 0; i < count; i++)
				{
					var attributes = new AttributesCollection();

					StressEntity.CalculateRandomCoords(area, out Vector3 position, out float angle);

					attributes.Set(0, position);
					attributes.Set(1, angle);
					attributes.Set(2, area);

					NetworkAPI.Client.Entities.Spawn(prefab, attributes: attributes);

					if (i % 50 == 0) yield return new WaitForEndOfFrame();
				}
			}
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(NetworkEntityStresser))]
		class Inspector : Editor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				EditorGUILayout.Space();

				GUI.enabled = Application.isPlaying;

				if (GUILayout.Button("Spawn"))
				{
					var target = base.target as NetworkEntityStresser;

					target.Perform();
				}
			}
		}
#endif
	}
}