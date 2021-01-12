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

namespace MNet
{
    public static partial class NetworkAPI
    {
        public static NetworkAPIConfig Config { get; private set; }

        public static NetworkSpawnableObjects SpawnableObjects { get; private set; }

        public static string Address => Config.MasterAddress;

        public static AppID AppID { get; private set; }

        public static Version GameVersion { get; private set; }

        public static bool IsRunning { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnLoad()
        {
            Config = NetworkAPIConfig.Load();

            if (Config == null)
                throw new Exception("No Network API Config ScriptableObject Found, Please Make Sure One is Created and Located in a Resources Folder");

            SpawnableObjects = NetworkSpawnableObjects.Load();

            if (SpawnableObjects == null)
                throw new Exception("No Network Spawnable Objects ScriptableObject Found, Please Make Sure One is Created and Located in a Resources Folder");

            Configure();
        }

        static void Configure()
        {
            Log.Output = LogOutput;

            GlobalCoroutine.Configure();

#if ENABLE_IL2CPP
            DynamicNetworkSerialization.Enabled = false;
#else
            DynamicNetworkSerialization.Enabled = true;
#endif

            ParseAppID();

            GameVersion = Config.GameVersion;

            IsRunning = true;

            Application.quitting += ApplicationQuitCallback;

            Server.Configure();
            Realtime.Configure();
            AppAPI.Configure();
            Client.Configure();
            Physics.Configure();
            Ping.Configure();
            Time.Configure();
            Room.Configure();

            RegisterUpdateMethods();
        }

        static void ParseAppID()
        {
            if (string.IsNullOrEmpty(Config.AppID))
                throw new Exception("Please Enter an App ID in Network API Config");

            if (Guid.TryParse(Config.AppID, out var guid) == false)
                throw new Exception($"Couldn't Parse '{Config.AppID}' as App ID, Please Enter a Valid App ID in Network API Config");

            AppID = new AppID(guid);
        }

        static void RegisterUpdateMethods()
        {
            if (Application.isEditor || Config.UpdateMethod.Early) RegisterPlayerLoop<EarlyUpdate>(EarlyUpdate);
            if (Application.isEditor || Config.UpdateMethod.Normal) RegisterPlayerLoop<Update>(Update);
            if (Application.isEditor || Config.UpdateMethod.Fixed) RegisterPlayerLoop<FixedUpdate>(FixedUpdate);

            if (Application.isEditor || Config.UpdateMethod.Late.Pre) RegisterPlayerLoop<PreLateUpdate>(PreLateUpdate);
            if (Application.isEditor || Config.UpdateMethod.Late.Post) RegisterPlayerLoop<PostLateUpdate>(PostLateUpdate);

            if (Config.UpdateMethod.Any == false)
                Debug.LogWarning("No Update Methods Selected in Network API Config, Network Message's Won't be Processed");
        }

        #region Updates
        static void EarlyUpdate()
        {
#if UNITY_EDITOR
            if (Config.UpdateMethod.Early == false) return;
#endif

            Process();
        }

        static void Update()
        {
#if UNITY_EDITOR
            if (Config.UpdateMethod.Normal == false) return;
#endif

            Process();
        }

        static void FixedUpdate()
        {
#if UNITY_EDITOR
            if (Config.UpdateMethod.Fixed == false) return;
#endif

            Process();
        }

        static void PreLateUpdate()
        {
#if UNITY_EDITOR
            if (Config.UpdateMethod.Late.Pre == false) return;
#endif

            Process();
        }

        static void PostLateUpdate()
        {
#if UNITY_EDITOR
            if (Config.UpdateMethod.Late.Post == false) return;
#endif

            Process();
        }
        #endregion

        public static event Action OnProcess;
        public static void Process()
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false) return;
#endif

            OnProcess?.Invoke();
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

        static void ApplicationQuitCallback()
        {
            Application.quitting -= ApplicationQuitCallback;

            IsRunning = false;
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