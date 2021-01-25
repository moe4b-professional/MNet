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
	[CreateAssetMenu]
	public class Core : ScriptableObject
	{
		public const int ExecutionOrder = -400;

		public static Core Instance { get; protected set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void OnLoad()
		{
			var assets = Resources.LoadAll<Core>("");

			if (assets.Length == 0) throw new Exception("No Core Asset Found");

			Instance = assets[0];

			Instance.Configure();
			Instance.Init();
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

			[SerializeField]
			GameScene additiveScene = default;
			public int AdditiveScene => additiveScene;

			UIProperty UI => Core.UI;

			protected override void Configure()
			{
				base.Configure();

				NetworkAPI.Room.Scenes.LoadMethod = Load;
			}

			public virtual void LoadMainMenu() => Load(mainMenu, LoadSceneMode.Single).Forget();

			async UniTask Load(GameScene scene, LoadSceneMode mode)
			{
				var array = new byte[] { (byte)scene.BuildIndex };

				await Load(array, mode);
			}

			async UniTask Load(byte[] scenes, LoadSceneMode mode)
			{
				if (mode == LoadSceneMode.Single)
				{
					await UI.Fader.Transition(1f, 0.2f);

					await UniTask.Delay(400);
				}

				await NetworkAPI.Room.Scenes.DefaultLoadMethod(scenes, mode);

				if (mode == LoadSceneMode.Single)
				{
					UI.Fader.Transition(0f, 0.2f).Forget();
				}
			}
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

			public LevelData this[int index] => list[index];

			public Dictionary<string, LevelData> Dictionary { get; protected set; }

			protected override void Configure()
			{
				base.Configure();

				Dictionary = list.ToDictionary(LevelData.GetName);
			}

			#region Attribute
			public void WriteAttribute(AttributesCollection attributes, byte index)
			{
				attributes.Set(0, index);
			}

			public LevelData ReadAttribute(AttributesCollection attributes)
			{
				if (attributes.TryGetValue(0, out byte index) == false)
					throw new Exception("Failed to Retrive Level from Room Attribute");

				var data = Core.Levels[index];

				return data;
			}
			#endregion
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

			NetworkClientProfile GenerateProfile() => new NetworkClientProfile(PlayerName);

			PopupPanel Popup => Core.UI.Popup;

			protected override void Configure()
			{
				base.Configure();

				NetworkAPI.Configure();

				if (PlayerPrefs.HasKey(PlayerNameKey) == false) PlayerName = GetDefaultPlayerName();

				NetworkAPI.Client.Register.GetProfileMethod = GenerateProfile;

				NetworkAPI.Client.Register.OnCallback += RegisterCallback;

				Core.OnInit += Init;
			}

			void Init()
			{
				GetMasterConfig();
			}

			void GetMasterConfig()
			{
				Popup.Show("Getting Master Scheme");

				NetworkAPI.Server.Master.GetScheme(Callback);

				void Callback(MasterServerSchemeResponse response, RestError error)
				{
					if (error == null)
					{
						GetMasterInfo();
						return;
					}

					Popup.Show("Failed To Retrieve Master Scheme", "Retry", GetMasterConfig);
				}
			}

			void GetMasterInfo()
			{
				Popup.Show("Getting Master Info");

				NetworkAPI.Server.Master.GetInfo(Callback);

				void Callback(MasterServerInfoResponse info, RestError error)
				{
					if (error == null)
					{
						Popup.Hide();
						return;
					}

					Popup.Show("Failed to Retrieve Master Info", "Retry", GetMasterInfo);
				}
			}

			void RegisterCallback(RegisterClientResponse response)
			{
				if (NetworkAPI.Client.IsMaster)
				{
					var level = Core.levels.ReadAttribute(NetworkAPI.Room.Info.Attributes);

					NetworkAPI.Room.Scenes.Load(LoadSceneMode.Single, level.Scene);
				}

				Popup.Hide();
			}
		}

		[SerializeField]
		UIProperty _UI = new UIProperty();
		public UIProperty UI => _UI;
		[Serializable]
		public class UIProperty : Property
		{
			[SerializeField]
			GameObject prefab = default;

			public CoreUIContainer Container { get; protected set; }

			public PopupPanel Popup => Container.Popup;

			public FaderUI Fader => Container.Fader;

			protected override void Configure()
			{
				base.Configure();

				var gameObject = Instantiate(prefab);
				gameObject.name = prefab.name;
				DontDestroyOnLoad(gameObject);

				Container = gameObject.GetComponent<CoreUIContainer>();
				Initializer.Configure(Container);

				Core.OnInit += Init;
			}

			void Init()
			{
				Initializer.Init(Container);
			}
		}

		[Serializable]
		public class Property
		{
			public static Core Core => Core.Instance;

			/// <summary>
			/// Use to call coroutines and other things that require a monobehaviour
			/// </summary>
			public static SceneAccessor SceneAccessor => Core.SceneAccessor;

			public static Coroutine StartCoroutine(IEnumerator routine) => SceneAccessor.StartCoroutine(routine);

			protected virtual void Configure()
			{

			}

			internal static void Configure(Property property) => property.Configure();
		}

		public void ForAllProperties(Action<Property> action)
		{
			action(scenes);
			action(levels);
			action(network);
			action(UI);
		}

		/// <summary>
		/// Use to call coroutines and other things that require a monobehaviour
		/// </summary>
		public SceneAccessor SceneAccessor { get; protected set; }

		void Configure()
		{
			SceneAccessor = SceneAccessor.Create();

			ForAllProperties(Property.Configure);
		}

		public event Action OnInit;
		void Init()
		{
			OnInit?.Invoke();
		}

		void Update()
		{

		}
	}
}