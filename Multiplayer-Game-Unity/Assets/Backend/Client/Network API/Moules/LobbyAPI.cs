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
using System.Net;

namespace Backend
{
	public static partial class NetworkAPI
	{
        public static class Lobby
        {
            #region Info
            public delegate void InfoDelegate(LobbyInfo lobby, RestError error);
            public static event InfoDelegate OnInfo;

            public static void Info(GameServerID id) => Info(id.ToString());
            public static void Info(string address)
            {
                GameServer.Rest.GET(address, Constants.GameServer.Rest.Requests.Lobby.Info, Callback, false);

                void Callback(UnityWebRequest request)
                {
                    RestAPI.Parse(request, out LobbyInfo info, out var error);

                    OnInfo(info, error);
                }
            }
            #endregion
        }
    }
}