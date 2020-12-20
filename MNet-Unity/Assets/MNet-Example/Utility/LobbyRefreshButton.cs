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
	[RequireComponent(typeof(Button))]
	public class LobbyRefreshButton : MonoBehaviour
	{
		Button button;

		Core Core => Core.Instance;
		PopupPanel Popup => Core.UI.Popup;

		void Start()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(Action);

			NetworkAPI.Lobby.OnInfo += Callback;
		}

		void Action()
		{
			NetworkAPI.Lobby.RequestInfo();

			button.interactable = false;
		}

		void Callback(LobbyInfo lobby, RestError error)
		{
			button.interactable = true;
		}

		void OnDestroy()
		{
			NetworkAPI.Lobby.OnInfo -= Callback;
		}
	}
}