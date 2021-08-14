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
                public static Dictionary<GameServerID, GameServerInfo> Collection { get; private set; }

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

                public static GameServerInfo Info => Collection[ID];

                public static RestClientAPI Rest { get; private set; }

                internal static void Configure()
                {
                    Rest = new RestClientAPI(Constants.Server.Game.Rest.Port, NetworkAPI.Config.RestScheme);

                    Collection = new Dictionary<GameServerID, GameServerInfo>();

                    Master.OnInfo += MasterInfoCallback;
                }

                static void MasterInfoCallback(MasterServerInfoResponse info)
                {
                    Register(info.Servers);
                }

                public delegate void RegisterDelegate();
                public static event RegisterDelegate OnRegister;
                static void Register(IList<GameServerInfo> list)
                {
                    Collection.Clear();

                    for (int i = 0; i < list.Count; i++) Collection.Add(list[i].ID, list[i]);

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