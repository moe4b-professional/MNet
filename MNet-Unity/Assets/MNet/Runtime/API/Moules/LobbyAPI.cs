﻿using System;
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

using Cysharp.Threading.Tasks;

namespace MNet
{
    public static partial class NetworkAPI
    {
        public static class Lobby
        {
            public static LobbyInfo Info { get; private set; }

            public static IList<RoomInfo> Rooms => Info.Rooms;

            public static int Size => Info.Size;

            public delegate void InfoDelegate(LobbyInfo lobby);
            public static event InfoDelegate OnInfo;
            public static async UniTask<LobbyInfo> GetInfo()
            {
                var payload = new GetLobbyInfoRequest(AppID, GameVersion);

                Info = await Server.Game.Rest.POST<GetLobbyInfoRequest, LobbyInfo>(Constants.Server.Game.Rest.Requests.Lobby.Info, payload);

                OnInfo?.Invoke(Info);

                return Info;
            }

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