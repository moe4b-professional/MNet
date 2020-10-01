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

using UnityEngine.Networking;

namespace Backend
{
	public static partial class NetworkAPI
	{
        public static partial class GameServer
        {
            public static GenericRestAPI Rest { get; private set; }

            public static void Configure()
            {
                Rest = new GenericRestAPI(Constants.GameServer.Rest.Port);
            }
        }
    }
}