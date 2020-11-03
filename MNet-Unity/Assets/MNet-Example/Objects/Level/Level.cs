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
using System.Net.Cache;

namespace MNet.Example
{
	[DefaultExecutionOrder(ExecutionOrder)]
	public class Level : NetworkBehaviour
	{
		public const int ExecutionOrder = -200;

		public static Level Instance { get; protected set; }

		public LevelUI UI { get; protected set; }

		static Core Core => Core.Instance;
		PopupPanel Popup => Core.UI.Popup;

		void Awake()
		{
			if (NetworkAPI.Client.IsConnected == false)
			{
				Core.Scenes.LoadMainMenu();
				return;
			}

			Instance = this;

			UI = FindObjectOfType<LevelUI>();
		}

		public void Quit()
		{
			if (NetworkAPI.Client.IsConnected)
			{
				Popup.Show("Disconnecting");

				NetworkAPI.Client.OnDisconnect += Callback;
				NetworkAPI.Client.Disconnect();
			}
			else
			{
				Core.Scenes.LoadMainMenu();
			}

			void Callback(DisconnectCode code)
			{
				NetworkAPI.Client.OnDisconnect -= Callback;

				Popup.Hide();

				Core.Scenes.LoadMainMenu();
			}
		}
	}
}