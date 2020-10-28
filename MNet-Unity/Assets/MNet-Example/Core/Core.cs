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
using WebSocketSharp;

namespace MNet.Example
{
	[DefaultExecutionOrder(ExecutionOrder)]
	public class Core : MonoBehaviour
	{
		public const int ExecutionOrder = -400;

		public static Core Instance { get; protected set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void OnLoad()
		{
			var prefab = Resources.Load<GameObject>(nameof(Core));

			var gameObject = Instantiate(prefab);

			gameObject.name = prefab.name;
			DontDestroyOnLoad(gameObject);
		}

		[SerializeField]
		ScenesProperty scenes = new ScenesProperty();
		public ScenesProperty Scenes => scenes;
		[Serializable]
		public class ScenesProperty : Property
		{
			[SerializeField]
			GameScene mainMenu = default;
			public GameScene MainMenu => mainMenu;

			public virtual void LoadMainMenu() => SceneManager.LoadScene(mainMenu.name);
		}

		[SerializeField]
		LevelsProperty levels = new LevelsProperty();
		public LevelsProperty Levels => levels;
		[Serializable]
		public class LevelsProperty : Property
		{
			[SerializeField]
			LevelData[] list = new LevelData[] { };
			public LevelData[] List => list;

			public Dictionary<string, LevelData> Dictionary { get; protected set; }

			protected override void Configure()
			{
				base.Configure();

				Dictionary = list.ToDictionary(LevelData.GetName);
			}

			public LevelData Find(string name)
			{
				if (Dictionary.TryGetValue(name, out var data))
					return data;

				return null;
			}

			public Coroutine Load(LevelData level)
			{
				return StartCoroutine(Procedure());

				IEnumerator Procedure()
				{
					var operation = SceneManager.LoadSceneAsync(level.Scene.name, LoadSceneMode.Single);
					operation.allowSceneActivation = false;

					bool IsLoad() => operation.progress == 0.9f;
					yield return new WaitUntil(IsLoad);

					operation.allowSceneActivation = true;
				}
			}
		}

		[SerializeField]
		NetworkProperty network = new NetworkProperty();
		public NetworkProperty Network => network;
		[Serializable]
		public class NetworkProperty : Property
		{
			[SerializeField]
			byte[] capacities = new byte[] { 2, 4, 6, 8 };
			public byte[] Capacities => capacities;

			public string PlayerName
			{
				get => PlayerPrefs.GetString(PlayerNameKey, "Player");
				set => PlayerPrefs.SetString(PlayerNameKey, value);
			}

			public const string PlayerNameKey = "Player Name";

			public static string GetDefaultPlayerName()
            {
				if (string.IsNullOrEmpty(Environment.UserName) == false)
					return Environment.UserName;

				return $"Player {Random.Range(0, 1000)}";
			}

			PopupPanel Popup => Core.UI.Popup;

			protected override void Configure()
			{
				base.Configure();

				if (PlayerPrefs.HasKey(PlayerNameKey) == false) PlayerName = GetDefaultPlayerName();

				NetworkAPI.Client.AutoRegister = false;
				NetworkAPI.Client.AutoReady = false;

				NetworkAPI.Client.OnConnect += ClientConnectCallback;
				NetworkAPI.Client.OnRegister += ClientRegisterCallback;

				Core.OnStart += Start;
			}

            void Start()
			{
				
			}

			void ClientConnectCallback()
			{
				NetworkAPI.Client.Profile = new NetworkClientProfile(PlayerName);

				NetworkAPI.Client.Register();
			}

			void ClientRegisterCallback(RegisterClientResponse response)
			{
				StartCoroutine(Procedure());

				IEnumerator Procedure()
				{
					var attributes = response.Room.Basic.Attributes;

					var level = ReadLevel(attributes);

					yield return Core.Levels.Load(level);

					Popup.Hide();

					NetworkAPI.Client.Ready();
				}
			}

			public LevelData ReadLevel(AttributesCollection attributes)
			{
				if (attributes.TryGetValue<string>(0, out var name) == false)
					throw new Exception("Failed to Retrive Level from room Attribute");

				var data = Core.Levels.Find(name);

				if (data == null)
					throw new Exception("No Level Data found for Level with Name '{name}'");

				return data;
			}
		}

		[SerializeField]
		UIProperty _UI = new UIProperty();
		public UIProperty UI => _UI;
		[Serializable]
		public class UIProperty : Property
		{
			[SerializeField]
			PopupPanel popup = default;
			public PopupPanel Popup => popup;
		}

		[Serializable]
		public class Property
		{
			public static Core Core => Core.Instance;

			public static Coroutine StartCoroutine(IEnumerator routine) => Core.StartCoroutine(routine);

			protected virtual void Configure()
			{

			}

			public static void Configure(Property property) => property.Configure();
		}

		public void ForAllProperties(Action<Property> action)
		{
			action(levels);
			action(network);
			action(UI);
		}

		void Awake()
		{
			Instance = this;

			Initializer.Configure(gameObject);

			ForAllProperties(Property.Configure);
		}

		public event Action OnStart;
		void Start()
		{
			Initializer.Init(gameObject);

			OnStart?.Invoke();
		}

		void Update()
		{

		}
	}
}