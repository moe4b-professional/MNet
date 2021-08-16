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

using MB;

namespace MNet.Example
{
	[CreateAssetMenu]
	public class Core : ScriptableObject
	{
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
			MSceneAsset mainMenu = default;
			public MSceneAsset MainMenu => mainMenu;

			UIProperty UI => Core.UI;

			protected internal override void Configure()
			{
				base.Configure();

				NetworkAPI.Room.Scenes.Load.ProcdureMethod = Load;
			}

			public virtual void LoadMainMenu() => Load(mainMenu, LoadSceneMode.Single).Forget();

			async UniTask Load(MSceneAsset scene, LoadSceneMode mode)
			{
				var index = (byte)scene.Index;

				await Load(index, mode);
			}

			async UniTask Load(byte scene, LoadSceneMode mode)
			{
				if (mode == LoadSceneMode.Single)
				{
					await UI.Fader.Transition(1f, 0.2f);

					await UniTask.Delay(400);
				}

				await NetworkAPI.Room.Scenes.Load.DefaultProcedure(scene, mode);

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

			protected internal override void Configure()
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
				get => AutoPrefs.Read("Player Name", "Player");
				set
				{
					AutoPrefs.Set("Player Name", value);
					NetworkAPI.Client.Name = value;
				}
			}

			PopupPanel Popup => Core.UI.Popup;

			protected internal override void Configure()
			{
				base.Configure();

				NetworkAPI.Configure();

				NetworkAPI.Client.Register.OnCallback += ClientRegisterCallback;
			}

			protected internal override void Init()
			{
				base.Init();

				if (AutoPrefs.Contains("Player Name") == false)
					PlayerName = $"Player {Random.Range(0, 1000)}";

				NetworkAPI.Client.Name = PlayerName;

				Process().Forget();
			}

			async UniTask Process()
			{
				await GetMasterScheme();
				await GetMasterInfo();

				Popup.Hide();
			}
			async UniTask GetMasterScheme()
			{
				Start: //Yes, your eyes aren't deceiving you, I declared a goto stamement
				Popup.Show("Getting Master Scheme");

				try
				{
					await NetworkAPI.Server.Master.GetScheme();
				}
				catch (Exception ex) when (ex is UnityWebRequestException)
				{
					Debug.LogError(ex);
					await Popup.Show("Failed To Retrieve Master Scheme", "Retry");

					goto Start; //Yes! I used it too
				}
			}
			async UniTask GetMasterInfo()
			{
				Start: //Yep, one more time
				Popup.Show("Getting Master Info");

				try
				{
					await NetworkAPI.Server.Master.GetInfo();
				}
				catch (Exception ex) when (ex is UnityWebRequestException)
				{
					Debug.LogError(ex);
					await Popup.Show("Failed to Retrieve Master Info", "Retry");

					goto Start; //Oh, the humanity!
				}
			}

			void ClientRegisterCallback(RegisterClientResponse response)
			{
				if (NetworkAPI.Client.IsMaster)
				{
					var level = Core.levels.ReadAttribute(NetworkAPI.Room.Info.Attributes);

					NetworkAPI.Room.Scenes.Load.Request(level.Scene, LoadSceneMode.Single);
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

			public TextInputPanel TextInput => Container.TextInput;

			public FaderUI Fader => Container.Fader;

			protected internal override void Configure()
			{
				base.Configure();

				var gameObject = Instantiate(prefab);
				gameObject.name = prefab.name;
				DontDestroyOnLoad(gameObject);

				Container = gameObject.GetComponent<CoreUIContainer>();
				Initializer.Configure(Container);
			}

			protected internal override void Init()
			{
				base.Init();

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

			protected internal virtual void Configure()
			{

			}

			protected internal virtual void Init()
			{

			}
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
			AutoPrefs.Configure();

			try
			{
				AutoPrefs.Load();
			}
			catch (Exception ex)
			{
				Debug.LogError($"Exception when Loading AutoPrefs, Will Reset Prefs" +
					$"{Environment.NewLine}" +
					$"Exception: {ex}");

				AutoPrefs.Reset();
			}

			SceneAccessor = SceneAccessor.Create();

			GlobalCoroutine.Configure();

			ForAllProperties(x => x.Configure());
		}

		void Init()
		{
			ForAllProperties(x => x.Init());
		}
	}
}