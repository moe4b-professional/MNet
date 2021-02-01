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
	[RequireComponent(typeof(Dropdown))]
	public class ServerBrowserRegionQueryDropDown : MonoBehaviour
	{
		[SerializeField]
		ServerBrowser browser = default;

		Dropdown dropdown;

		List<GameServerRegion> options;

		bool Predicate(GameServerInfo info)
        {
			if (dropdown.value == 0)
				return true;

			return info.Region == options[dropdown.value - 1];
		}

		void Awake()
        {
			dropdown = GetComponent<Dropdown>();
			dropdown.ClearOptions();

			dropdown.AddOptions("Any");

			options = new List<GameServerRegion>();

			foreach (GameServerRegion region in Enum.GetValues(typeof(GameServerRegion)))
				options.Add(region);

			dropdown.AddOptions(options);
		}

		void Start()
        {
			browser.AddQuery(Predicate);

			dropdown.onValueChanged.AddListener(ChangeCallback);
		}

		void ChangeCallback(int value) => browser.UpdateQuery();
	}
}