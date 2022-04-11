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
                public static Dictionary<GameServerID, GameServerInfo> Dictionary { get; private set; }

                public static ICollection<GameServerInfo> Collection => Dictionary.Values;

                public static GameServerID? Selection { get; private set; }

                public static GameServerID ID
                {
                    get
                    {
                        if (Selection == null)
                            throw new Exception("No Game Server Selected, Please Select a Game Server Before Using it's Methods");

                        return Selection.Value;
                    }
                }

                public static GameServerInfo Info => Dictionary[ID];

                public static RestClientAPI Rest { get; private set; }

                internal static void Configure()
                {
                    Rest = new RestClientAPI(Constants.Server.Game.Rest.Port, NetworkAPI.Config.RestScheme);

                    Dictionary = new Dictionary<GameServerID, GameServerInfo>();
                }

                public delegate void RegisterDelegate();
                public static event RegisterDelegate OnRegister;
                internal static void Register(IList<GameServerInfo> list)
                {
                    Dictionary.Clear();

                    for (int i = 0; i < list.Count; i++) Dictionary.Add(list[i].ID, list[i]);

                    OnRegister?.Invoke();
                }

                public delegate void SelectDelegate(GameServerID id);
                public static event SelectDelegate OnSelect;
                public static void Select(GameServerID id)
                {
                    Selection = id;

                    Rest.SetIP(id.Address);

                    OnSelect?.Invoke(id);
                }
                public static void Select(GameServerInfo info) => Select(info.ID);
            }
        }
    }
}