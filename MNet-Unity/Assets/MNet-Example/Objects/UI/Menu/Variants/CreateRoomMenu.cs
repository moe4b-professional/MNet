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
	public class CreateRoomMenu : UIElement
	{
		[SerializeField]
		new InputField name = null;

		[SerializeField]
		Dropdown capacity = null;

		[SerializeField]
		Dropdown level = null;

		[SerializeField]
		Button button = null;

		public Core Core => Core.Instance;
		PopupPanel Popup => Core.UI.Popup;

		void Awake()
		{
			button.onClick.AddListener(Perform);

			capacity.ClearOptions();
			capacity.AddOptions(Core.Network.Capacities);

			level.ClearOptions();
			level.AddOptions(Core.Levels.List);
		}

		void Perform()
		{
			var name = this.name.GetValueOrDefault("Game Room");
			var capacity = this.capacity.GetOption(Core.Network.Capacities);
			var level = this.level.GetOption(Core.Levels.List);

			Create(name, capacity, level);
		}

		void Create(string name, byte capacity, LevelData level)
		{
			Popup.Show("Creating Room");

			var attributes = new AttributesCollection();

			attributes.Set(0, level.Name);

			NetworkAPI.Room.OnCreate += CreateCallback;
			NetworkAPI.Room.Create(name, capacity, attributes);
		}
		void CreateCallback(RoomBasicInfo room, RestError error)
		{
			NetworkAPI.Room.OnCreate -= CreateCallback;

			if (error == null)
				Join(room);
			else
				Popup.Show("Failed to Create Room", "Okay");
		}

		void Join(RoomBasicInfo info)
        {
			Popup.Show("Joining Room");

			NetworkAPI.Room.Join(info);
		}
	}
}