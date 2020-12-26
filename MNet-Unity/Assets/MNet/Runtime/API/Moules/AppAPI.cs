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
		public static class AppAPI
		{
			public static AppConfig Config { get; private set; }

			public static void Configure()
            {
                Server.Master.OnScheme += MasterServerSchemeCallback;
            }

            static void MasterServerSchemeCallback(MasterServerSchemeResponse response, RestError error)
            {
                if (error != null) return;

                Set(response.App);
            }

            public delegate void SetDelegate(AppConfig app);
			public static event SetDelegate OnSet;
			public static void Set(AppConfig instance)
            {
				Config = instance;

				OnSet?.Invoke(instance);
            }
		}
	}
}