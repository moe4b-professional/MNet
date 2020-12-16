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
		void Start()
        {
            if (Application.isEditor == false) Begin();
        }

        void Begin()
        {
            NetworkAPI.Server.Master.OnInfo += MasterServerInfoCallback;

            NetworkAPI.Lobby.OnInfo += LobbyInfoCallback;
        }

        void MasterServerInfoCallback(MasterServerInfoResponse info, RestError error)
        {
            if (error != null) return;

            NetworkAPI.Server.Game.Select(info.Servers[0]);

            NetworkAPI.Lobby.GetInfo();
        }

        void LobbyInfoCallback(LobbyInfo lobby, RestError error)
        {
            if (error != null) return;

            if (lobby.Size == 0) return;

            NetworkAPI.Room.Join(lobby.Rooms.Last());
        }
    }
}