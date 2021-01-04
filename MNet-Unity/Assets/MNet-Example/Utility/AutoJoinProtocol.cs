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

namespace MNet.Example
{
    public class AutoJoinProtocol : MonoBehaviour
    {
        static bool tried = false;

        void Start()
        {
            if (Application.isEditor == false) Begin();
        }

        void Begin()
        {
            if (tried) return;

            tried = true;

            NetworkAPI.Server.Game.OnSelect += GameServerSelectCallback;
            NetworkAPI.Lobby.OnInfo += LobbyInfoCallback;

            if (NetworkAPI.Server.Game.Collection.Count == 0)
            {
                NetworkAPI.Server.Master.OnInfo += MasterServerInfoCallback;
            }
            else
            {
                NetworkAPI.Server.Game.Select(NetworkAPI.Server.Game.Collection.FirstOrDefault().Value);
            }
        }

        void MasterServerInfoCallback(MasterServerInfoResponse info, RestError error)
        {
            if (error != null) return;

            NetworkAPI.Server.Game.Select(info.Servers[0]);
        }

        void GameServerSelectCallback(GameServerID id)
        {
            NetworkAPI.Lobby.GetInfo();
        }

        void LobbyInfoCallback(LobbyInfo lobby, RestError error)
        {
            if (error != null) return;

            if (lobby.Size == 0) return;

            NetworkAPI.Room.Join(lobby.Rooms.Last());
        }

        void OnDestroy()
        {
            NetworkAPI.Server.Master.OnInfo -= MasterServerInfoCallback;
            NetworkAPI.Lobby.OnInfo -= LobbyInfoCallback;
        }
    }
}