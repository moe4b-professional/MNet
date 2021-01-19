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

		protected override void OnSpawn()
		{
			base.OnSpawn();

			Player.Spawn(player);
		}

		public async UniTask Quit()
		{
			if (NetworkAPI.Client.IsConnected)
			{
				Popup.Show("Disconnecting");

				await UniTask.Delay(200);

				NetworkAPI.Client.Disconnect();

				await UniTask.Delay(500);

				Popup.Hide();
			}

			Core.Scenes.LoadMainMenu();
		}
	}
}