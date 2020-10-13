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
using System.Runtime.CompilerServices;

namespace MNet
{
	public static partial class NetworkAPI
	{
        public static partial class Server
        {
            public static partial class Game
            {
                public static GameServerID? Selection { get; private set; }

                public static bool HasSelection => Selection.HasValue;

                public static GameServerID ID
                {
                    get
                    {
                        if (Selection == null)
                            throw new Exception("No Game Server Selected, Please Select a Game Server Before Using it's Methods");

                        return Selection.Value;
                    }
                }

                public static DirectedRestAPI Rest { get; private set; }

                public static void Configure()
                {
                    Rest = new DirectedRestAPI(Constants.Server.Game.Rest.Port);
                }

                public static void Select(GameServerInfo info) => Select(info.ID);
                public static void Select(GameServerID id)
                {
                    Selection = id;

                    Rest.SetIP(id.Address);

                    Debug.Log($"Selecting Game Server: {id}");
                }
            }
        }
        
    }
}