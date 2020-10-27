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
	[RequireComponent(typeof(Text))]
	public class GameServerSelectionLabel : MonoBehaviour
	{
		Text label;

		void Start()
        {
			label = GetComponent<Text>();

			UpdateState();

            NetworkAPI.Server.Game.OnSelect += GameServerSelectCallback;
		}

		void GameServerSelectCallback(GameServerID id) => UpdateState();

		void UpdateState()
        {
			if (NetworkAPI.Server.Game.HasSelection)
				label.text = $"{NetworkAPI.Server.Game.Info.Name}";
			else
				label.text = $"None";
		}

		void OnDestroy()
        {
			NetworkAPI.Server.Game.OnSelect -= GameServerSelectCallback;
		}
	}
}