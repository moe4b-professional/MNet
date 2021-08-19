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
	public class CreateRoomMenu : UIElement
	{
		[SerializeField]
		new InputField name = default;

		[SerializeField]
		Dropdown capacity = default;

		[SerializeField]
		Dropdown level = default;

		[SerializeField]
		InputField password = default;

		[SerializeField]
		Toggle offline = default;

		[SerializeField]
		Button button = default;

		public Core Core => Core.Instance;
		PopupPanel Popup => Core.UI.Popup;

		void Awake()
		{
			button.onClick.AddListener(Perform);

			capacity.ClearOptions();
			capacity.AddOptions(Core.Network.Capacities);
			capacity.value = Core.Network.Capacities.Length - 1;

			level.ClearOptions();
			level.AddOptions(Core.Levels.List);
		}

		void Perform()
		{
			var name = this.name.GetValueOrDefault("Game Room");
			var capacity = this.capacity.GetOption(Core.Network.Capacities);
			var level = (byte)this.level.value;
			var offline = this.offline.isOn;
			var password = this.password.text;

			Create(name, capacity, password, level, offline).Forget();
		}

		async UniTask Create(string name, byte capacity, string password, byte level, bool offline)
		{
			Popup.Show("Creating Room");

			var attributes = new AttributesCollection();
			Core.Levels.WriteAttribute(attributes, level);

			RoomInfo info;

			try
			{
				var options = new RoomOptions()
				{
					Capacity = capacity,
					Visible = false,
					Password = password,
					MigrationPolicy = MigrationPolicy.Continue,
					Attributes = attributes,
				};

				info = await NetworkAPI.Room.Create(name, options, offline);
			}
			catch (Exception ex) when (ex is UnityWebRequestException)
			{
				Popup.Show("Failed to Create Room", "Okay").Forget();
				return;
			}

			Join(info, password);
		}

		void Join(RoomInfo info, string password)
        {
			Popup.Show("Joining Room");

			NetworkAPI.Room.Join(info, password);
		}
	}
}