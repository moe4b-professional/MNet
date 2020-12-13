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
	public class NetworkStresser : NetworkBehaviour
	{
		public int count = 2000;

		public int area = 50;

		IEnumerator Start()
		{
			if (NetworkAPI.Client.IsMaster == false) yield break;

			for (int i = 0; i < count; i++)
            {
				Spawn();

				if (i % 20 == 0) yield return new WaitForEndOfFrame();
			}
		}

		void Spawn()
		{
			var attributes = new AttributesCollection();

			NetworkStressBehaviour.CalculateRandomCoords(area, out Vector3 position, out float angle);

			attributes.Set(0, position);
			attributes.Set(1, angle);
			attributes.Set(2, area);

			NetworkAPI.Client.SpawnEntity("Stress Entity", attributes: attributes);
		}
	}
}