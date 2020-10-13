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

namespace MNet
{
    public static partial class NetworkAPI
    {
        public static NetworkAPIConfig Config { get; private set; }

        public static string Address => Config.Address;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void OnLoad()
        {
            Config = NetworkAPIConfig.Load();

            if (Config == null)
                throw new Exception("No Network API Config ScriptableObject Found, Please Make Sure One is Created and Located in a Resources Folder");

            RegisterPlayerLoop<Update>(Update);

            Configure();
        }

        static void Configure()
        {
            Log.Output = LogOutput;

            Server.Configure();

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

        static void RegisterPlayerLoop<TType>(PlayerLoopSystem.UpdateFunction callback)
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < loop.subSystemList.Length; ++i)
                if (loop.subSystemList[i].type == typeof(TType))
                    loop.subSystemList[i].updateDelegate += callback;

            PlayerLoop.SetPlayerLoop(loop);
        }
    }
}