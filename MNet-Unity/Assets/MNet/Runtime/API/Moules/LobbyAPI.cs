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

namespace MNet
{
    public static partial class NetworkAPI
    {
        public static class Lobby
        {
            public static LobbyInfo Info { get; private set; }

            public static IList<RoomBasicInfo> Rooms => Info.Rooms;

            public static int Size => Info.Size;

            #region Info
            public delegate void RequestInfoDelegate();
            public static event RequestInfoDelegate OnRequestInfo;
            public static void RequestInfo()
            {
                var payload = new GetLobbyInfoRequest(NetworkAPI.AppID, NetworkAPI.Version);

                OnRequestInfo?.Invoke();

                Server.Game.Rest.POST<GetLobbyInfoRequest, LobbyInfo>(Constants.Server.Game.Rest.Requests.Lobby.Info, payload, InfoCallback);
            }

            public delegate void InfoDelegate(LobbyInfo lobby, RestError error);
            public static event InfoDelegate OnInfo;
            static void InfoCallback(LobbyInfo info, RestError error)
            {
                Lobby.Info = info;

                OnInfo?.Invoke(info, error);
            }
            #endregion

            public static event Action OnClear;
            public static void Clear()
            {
                Info = default;

                OnClear?.Invoke();
            }

            static void GameServerSelectCallback(GameServerID id) => Clear();

            static Lobby()
            {
                Server.Game.OnSelect += GameServerSelectCallback;
            }
        }
    }
}