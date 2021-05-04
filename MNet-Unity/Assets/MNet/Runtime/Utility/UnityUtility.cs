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

using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace MNet
{
	public static class UnityUtility
	{
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnLoad()
        {
            LateStart.OnLoad();
        }

        public static RuntimePlatform CheckPlatform()
        {
#if UNITY_EDITOR
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneOSX:
                    return RuntimePlatform.OSXPlayer;

                case BuildTarget.StandaloneWindows:
                    return RuntimePlatform.WindowsPlayer;

                case BuildTarget.iOS:
                    return RuntimePlatform.IPhonePlayer;

                case BuildTarget.Android:
                    return RuntimePlatform.Android;

                case BuildTarget.StandaloneWindows64:
                    return RuntimePlatform.WindowsPlayer;

                case BuildTarget.WebGL:
                    return RuntimePlatform.WebGLPlayer;

                case BuildTarget.WSAPlayer:
                    return RuntimePlatform.WSAPlayerX64;

                case BuildTarget.StandaloneLinux64:
                    return RuntimePlatform.LinuxPlayer;

                case BuildTarget.PS4:
                    return RuntimePlatform.PS4;

                case BuildTarget.XboxOne:
                    return RuntimePlatform.XboxOne;

                case BuildTarget.tvOS:
                    return RuntimePlatform.tvOS;

                case BuildTarget.Switch:
                    return RuntimePlatform.Switch;

                case BuildTarget.Lumin:
                    return RuntimePlatform.Lumin;

                case BuildTarget.Stadia:
                    return RuntimePlatform.Stadia;
            }
#endif

            return Application.platform;
        }

        public static void RegisterPlayerLoop<TType>(PlayerLoopSystem.UpdateFunction callback)
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < loop.subSystemList.Length; ++i)
                if (loop.subSystemList[i].type == typeof(TType))
                    loop.subSystemList[i].updateDelegate += callback;

            PlayerLoop.SetPlayerLoop(loop);
        }

        public static T GetComponentInParents<T>(Component component) => GetComponentInParents<T>(component.transform);
        public static T GetComponentInParents<T>(GameObject gameObject) => GetComponentInParents<T>(gameObject.transform);
        public static T GetComponentInParents<T>(Transform transform)
            where T : class
        {
            var context = transform.parent;

            while(true)
            {
                if (context == null) return null;

                if (context.TryGetComponent<T>(out var component))
                    return component;

                context = context.parent;
            }
        }

        public static class LateStart
        {
            public static Queue<Action> Queue { get; private set; }

            public static void Register(Action callback) => Queue.Enqueue(callback);

            internal static void OnLoad()
            {
                RegisterPlayerLoop<PreLateUpdate>(PreLateUpdate);
            }

            static void PreLateUpdate()
            {
                while (Queue.Count > 0)
                {
                    var callback = Queue.Dequeue();

                    if (callback == null) continue;

                    callback();
                }
            }

            static LateStart()
            {
                Queue = new Queue<Action>();
            }
        }
    }
}