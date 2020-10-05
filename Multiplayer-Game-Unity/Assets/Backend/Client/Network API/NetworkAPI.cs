using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
#endif

using Object = UnityEngine.Object;

using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Networking;
using Game;

namespace Backend
{
    public static partial class NetworkAPI
    {
        public static NetworkAPIConfig Config { get; private set; }

        public static string Address => Config.Address;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnLoad()
        {
            Config = Resources.Load<NetworkAPIConfig>("Network API Config");

            if (Config == null)
                throw new Exception("No Network API Config ScriptableObject Found, Please Make Sure One is Created and Located in a Resources Folder");

            var loop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < loop.subSystemList.Length; ++i)
                if (loop.subSystemList[i].type == typeof(Update))
                    loop.subSystemList[i].updateDelegate += Update;

            PlayerLoop.SetPlayerLoop(loop);
        }

        public static void Configure()
        {
            Log.Output = LogOutput;

            MasterServer.Configure();
            GameServer.Configure();

            Client.Configure();
            RealtimeAPI.Configure();

            Room.Configure();
        }

        static void LogOutput(object target, Log.Level level)
        {
            switch (level)
            {
                case Log.Level.Info:
                    Debug.Log(target);
                    break;

                case Log.Level.Warning:
                    Debug.LogWarning(target);
                    break;

                case Log.Level.Error:
                    Debug.LogError(target);
                    break;

                default:
                    Debug.LogWarning($"No Logging Case Made For Log Level {level}");
                    Debug.Log(target);
                    break;
            }
        }

        public static event Action OnUpdate;
        public static void Update()
        {
            OnUpdate?.Invoke();
        }
    }
}