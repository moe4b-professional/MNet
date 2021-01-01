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

using System.Threading.Tasks;

namespace MNet
{
	public static partial class NetworkAPI
	{
		public static class Scenes
		{
			public static Scene Active => SceneManager.GetActiveScene();

			internal static void Configure()
            {
				Client.RegisterMessageHandler<LoadScenesCommand>(Load);
			}

			#region Load
			public static bool IsLoading { get; private set; } = false;

			static readonly object RealtimePauseLock = new object();

			public static event LoadDelegate OnLoadBegin;

			static async void Load(LoadScenesCommand command)
			{
				if (IsLoading) throw new Exception("Scene API Already Loading Scene Recieved new Load Scene Command While Already Loading a Previous Command");

				var scenes = command.Scenes;
				var mode = ConvertLoadMode(command.Mode);

				IsLoading = true;
				Realtime.Pause.AddLock(RealtimePauseLock);
				OnLoadBegin?.Invoke(scenes, mode);

				if (mode == LoadSceneMode.Single) DestoryNonPersistantEntities();

				for (int i = 0; i < scenes.Length; i++)
				{
					var scene = SceneManager.GetSceneByBuildIndex(scenes[i]);

					if (scene.isLoaded)
					{
						Log.Warning($"Got Command to Load Scene at Index {scenes[i]} but That Scene is Already Loaded, " +
							$"Loading The Same Scene Multiple Times is not Supported, Ignoring");
						continue;
					}

					var operation = SceneManager.LoadSceneAsync(scenes[i], mode);

					while (operation.isDone == false) await Task.Delay(30);

					if (i == 0) mode = LoadSceneMode.Additive;
				}

				IsLoading = false;
				Realtime.Pause.RemoveLock(RealtimePauseLock);
				OnLoadEnd?.Invoke(scenes, mode);
			}

			public static event LoadDelegate OnLoadEnd;

			public delegate void LoadDelegate(byte[] indexes, LoadSceneMode mode);
			#endregion

			static void DestoryNonPersistantEntities()
			{
				var entities = Room.Entities.Values.ToArray();

				for (int i = 0; i < entities.Length; i++)
				{
					if (entities[i].Persistance.HasFlag(PersistanceFlags.SceneLoad)) continue;

					Room.DestroyEntity(entities[i]);
				}
			}

            #region Request
            public static void Load(params GameScene[] scenes) => Load(LoadSceneMode.Single, scenes);
			public static void Load(LoadSceneMode mode, params GameScene[] scenes)
			{
				var indexes = Array.ConvertAll(scenes, Convert);

				byte Convert(GameScene element)
				{
					if (element.BuildIndex > byte.MaxValue)
						throw new Exception($"Trying to Load at Build Index {element.BuildIndex}, Maximum Allowed Build Index is {byte.MaxValue}");

					return (byte)element.BuildIndex;
				}

				Load(mode, indexes);
			}

			public static void Load(params byte[] indexes) => Load(LoadSceneMode.Single, indexes);
			public static void Load(LoadSceneMode mode, params byte[] indexes)
			{
				if (Client.IsMaster == false)
				{
					Debug.LogWarning($"Only the Master Client Can Load Scenes, Ignoring Request");
					return;
				}

				var request = new LoadScenesRequest(indexes, ConvertLoadMode(mode));

				Client.Send(request);
			}
            #endregion

            static NetworkSceneLoadMode ConvertLoadMode(LoadSceneMode mode) => (NetworkSceneLoadMode)mode;
			static LoadSceneMode ConvertLoadMode(NetworkSceneLoadMode mode) => (LoadSceneMode)mode;

			internal static void MoveToActive(Component target) => MoveToActive(target.gameObject);
			internal static void MoveToActive(GameObject gameObject) => SceneManager.MoveGameObjectToScene(gameObject, Active);
		}
	}
}