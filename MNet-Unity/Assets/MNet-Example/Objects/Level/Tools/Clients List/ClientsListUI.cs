﻿using System;
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
	public class ClientsListUI : MonoBehaviour
	{
		[SerializeField]
		GameObject template = default;

		[SerializeField]
		ScrollRect scroll = default;

		UIElement panel;

		[SerializeField]
		Text count = default;

		Dictionary<NetworkClientID, ClientListUITemplate> dictionary;

		LevelPause Pause => Level.Instance.Pause;

		void Awake()
		{
			dictionary = new Dictionary<NetworkClientID, ClientListUITemplate>();

			panel = scroll.GetComponent<UIElement>();
		}

		void Start()
		{
			NetworkAPI.Room.OnAddClient += AddClientCallback;
			NetworkAPI.Room.OnRemoveClient += RemoveClientCallback;

			panel.Hide();

			UpdateState();
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.Tab)) Toggle();
		}

        #region Controls
        void Toggle()
		{
			if (Pause.Mode == LevelPauseMode.None && panel.Visible == false)
				Show();
			else if (Pause.Mode == LevelPauseMode.Soft && panel.Visible)
				Hide();
		}

		void Show()
		{
			Pause.Set(LevelPauseMode.Soft);
			panel.Show();
		}

		void Hide()
		{
			Pause.Set(LevelPauseMode.None);
			panel.Hide();
		}
		#endregion

		void UpdateState()
		{
			count.text = $"{NetworkAPI.Room.Clients.Count}/{NetworkAPI.Room.Info.Basic.Capacity}";
		}

		#region Add & Remove
		void AddClientCallback(NetworkClient client) => Add(client.ID, client.Profile);
		void Add(NetworkClientID id, NetworkClientProfile info)
		{
			var element = ClientListUITemplate.Create(template, info);

			element.SetParent(scroll.content);

			dictionary[id] = element;

			UpdateState();
		}

		void RemoveClientCallback(NetworkClient client) => Remove(client.ID);
		void Remove(NetworkClientID id)
		{
			if (dictionary.TryGetValue(id, out var element) == false) return;

			Destroy(element.gameObject);

			UpdateState();
		}
        #endregion

        void OnDestroy()
		{
			NetworkAPI.Room.OnAddClient -= AddClientCallback;
			NetworkAPI.Room.OnRemoveClient -= RemoveClientCallback;
		}
	}
}