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

namespace MNet
{
	public static partial class NetworkAPI
    {
        public static partial class Server
        {
            public static RemoteConfig RemoteConfig { get; private set; }

            internal static void Configure()
            {
                Master.Configure();
                Game.Configure();

                Master.OnScheme += MasterSchemeCallback;
            }

            static void MasterSchemeCallback(MasterServerSchemeResponse response)
            {
                SetRemoteConfig(response.RemoteConfig);
            }

            public delegate void RemoteConfigDelegate(RemoteConfig config);
            public static event RemoteConfigDelegate OnRemoteConfig;
            public static void SetRemoteConfig(RemoteConfig instance)
            {
                RemoteConfig = instance;

                OnRemoteConfig?.Invoke(instance);
            }
        }
    }
}