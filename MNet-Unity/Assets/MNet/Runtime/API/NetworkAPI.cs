using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;

using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

using MB;

namespace MNet
{
    public static partial class NetworkAPI
    {
        #region Config
        public static NetworkAPIConfig Config { get; private set; }

        public static AppID AppID => Config.AppID;

        public static string Address => Config.MasterAddress;

        public static Version GameVersion => Config.GameVersion;
        #endregion

        public static bool IsRunning { get; private set; }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void EditorInitialize()
        {
            Log.Output = LogOutput;
        }
#endif

        public static void Configure()
        {
            Config = NetworkAPIConfig.Load();

            if (Config == null)
                throw new Exception("No Network API Config ScriptableObject Found, Please Make Sure One is Created and Located in a Resources Folder");

            Config.Prepare();

            Log.Output = LogOutput;

#if ENABLE_IL2CPP
            DynamicNetworkSerialization.Enabled = false;
#else
            DynamicNetworkSerialization.Enabled = true;
#endif

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

        static void RegisterUpdateMethods()
        {
            if (Application.isEditor || Config.UpdateMethod.Early) MUtility.RegisterPlayerLoop<EarlyUpdate>(EarlyUpdate);
            if (Application.isEditor || Config.UpdateMethod.Normal) MUtility.RegisterPlayerLoop<Update>(Update);
            if (Application.isEditor || Config.UpdateMethod.Fixed) MUtility.RegisterPlayerLoop<FixedUpdate>(FixedUpdate);

            if (Application.isEditor || Config.UpdateMethod.Late.Pre) MUtility.RegisterPlayerLoop<PreLateUpdate>(PreLateUpdate);
            if (Application.isEditor || Config.UpdateMethod.Late.Post) MUtility.RegisterPlayerLoop<PostLateUpdate>(PostLateUpdate);

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
    }
}