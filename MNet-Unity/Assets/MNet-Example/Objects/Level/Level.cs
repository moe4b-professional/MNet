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
	public class Level : NetworkBehaviour
	{
		public static Level Instance { get; protected set; }

		public LevelUI UI { get; protected set; }

		static Core Core => Core.Instance;

		PopupPanel Popup => Core.UI.Popup;

		void Awake()
        {
			Instance = this;

			if (NetworkAPI.Client.IsConnected == false)
			{
				Core.Scenes.LoadMainMenu();
				return;
			}

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