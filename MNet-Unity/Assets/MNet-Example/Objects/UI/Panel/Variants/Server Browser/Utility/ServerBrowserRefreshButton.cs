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
	public class ServerBrowserRefreshButton : MonoBehaviour
	{
		[SerializeField]
		ServerBrowser browser = default;

		Button button;

		void Start()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(Action);

            NetworkAPI.Server.Master.OnInfo += Callback;
		}

        void Action()
		{
			button.interactable = false;

			browser.Refresh();
		}

		void Callback(MasterServerInfoResponse info, RestError error)
		{
			button.interactable = true;
		}

		void OnDestroy()
		{
			NetworkAPI.Server.Master.OnInfo -= Callback;
		}
	}
}