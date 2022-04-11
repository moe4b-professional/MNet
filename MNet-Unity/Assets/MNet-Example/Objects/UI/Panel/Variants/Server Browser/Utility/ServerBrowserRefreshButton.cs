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

namespace MNet.Example
{
	[RequireComponent(typeof(Button))]
	public class ServerBrowserRefreshButton : MonoBehaviour
	{
		[SerializeField]
		ServerBrowser browser = default;

		Button button;

		void Start()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(Action);
		}

		async void Action()
		{
			button.interactable = false;

			await browser.Refresh();

			button.interactable = true;
		}
	}
}