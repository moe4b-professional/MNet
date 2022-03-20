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

using Cysharp.Threading.Tasks;

using MB;

namespace MNet.Example
{
	[DefaultExecutionOrder(ExecutionOrder)]
	public class Level : NetworkBehaviour
	{
		public const int ExecutionOrder = -200;

		[SerializeField]
		PrefabAsset player = default;

		public static Level Instance { get; protected set; }

		public LevelUI UI { get; protected set; }
		public LevelPause Pause { get; protected set; }

		public RoomLog RoomLog { get; protected set; }

		static Core Core => Core.Instance;
		PopupPanel Popup => Core.UI.Popup;

        public override void OnNetwork()
        {
            base.OnNetwork();

            Network.OnSpawn += SpawnCallback;
        }

        void Awake()
		{
			Instance = this;

			UI = FindObjectOfType<LevelUI>();
			Pause = GetComponentInChildren<LevelPause>();

			RoomLog = GetComponentInChildren<RoomLog>();

			if (NetworkAPI.Client.IsConnected == false)
			{
				Core.Scenes.LoadMainMenu();
				return;
			}
		}

        void SpawnCallback()
		{
			if (NetworkAPI.Client.IsMaster)
			{
				NetworkAPI.Room.Info.Visible = true;
			}

			Player.Spawn(player, 4f);
		}

        public async UniTask Quit()
		{
			if (NetworkAPI.Client.IsConnected)
			{
				Popup.Show("Disconnecting");

				await UniTask.Delay(100);
				NetworkAPI.Client.Disconnect();
				await UniTask.Delay(100);

				Popup.Hide();
			}

			Core.Scenes.LoadMainMenu();
		}
	}
}