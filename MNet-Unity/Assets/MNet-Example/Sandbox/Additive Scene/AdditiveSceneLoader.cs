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
	public class AdditiveSceneLoader : NetworkBehaviour
	{
		[SerializeField]
		GameScene scene = default;

		[SerializeField]
		float delay = 2f;

		IEnumerator Start()
		{
			if (NetworkAPI.Client.IsMaster)
			{
				yield return new WaitForSeconds(delay);

				NetworkAPI.Scenes.Load(LoadSceneMode.Additive, scene);
			}
		}
	}
}