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

using WebSocketSharp;

using MNet;

namespace Game
{
	public class Sandbox : MonoBehaviour
	{
        void Start()
        {
            MNetAPI.Server.Master.OnInfo += MasterServerInfoCallback;

            MNetAPI.Lobby.OnInfo += LobbyInfoCallback;
            MNetAPI.Room.OnCreate += RoomCreateCallback;

            MNetAPI.Client.Profile = new NetworkClientProfile("Moe4B");
            MNetAPI.Client.OnReady += ClientReadyCallback;

            MNetAPI.Server.Master.Info();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                var attributes = new AttributesCollection();

                attributes.Set(0, "Level");

                MNetAPI.Room.Create("Moe4B's Game Room", 4, attributes);
            }

            if (Input.GetKeyDown(KeyCode.V)) MNetAPI.Lobby.GetInfo();

            if (Input.GetKeyDown(KeyCode.L)) MNetAPI.Client.Disconnect();

            if (Input.GetKeyDown(KeyCode.E)) SpawnPlayer();

            if (Input.GetKeyDown(KeyCode.Q)) DestroyEntity();
        }

        void MasterServerInfoCallback(MasterServerInfoResponse info, RestError error)
        {
            if (error == null)
            {
                Debug.Log($"Game Servers Count: {info.Size}");

                if (info.Size > 0)
                {
                    var server = info[0];

                    Debug.Log($"Selecting Game Server: {server}");

                    MNetAPI.Server.Game.Select(server);
                }
            }
            else
            {
                Debug.LogError(error);
            }
        }

        void LobbyInfoCallback(LobbyInfo lobby, RestError error)
        {
            if(error == null)
            {
                Debug.Log("Lobby Size: " + lobby.Size);

                var room = lobby.Rooms.FirstOrDefault();

                if (room == null) return;

                MNetAPI.Room.Join(room);
            }
            else
            {
                Debug.LogError(error);
            }
        }

        void RoomCreateCallback(RoomBasicInfo room, RestError error)
        {
            if (error == null)
            {
                Debug.Log("Created Room " + room.ID);

                MNetAPI.Room.Join(room);
            }
            else
            {
                Debug.LogError(error);
            }
        }

        void ClientReadyCallback(ReadyClientResponse response) => SpawnPlayer();

        void SpawnPlayer()
        {
            var attributes = new AttributesCollection();

            var position = new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
            var rotation = Quaternion.LookRotation(position.normalized);

            Player.Write(ref attributes, position, rotation);

            MNetAPI.Client.RequestSpawnEntity("Player", attributes);
        }

        void DestroyEntity()
        {
            if (MNetAPI.Client.Entities.Count > 0)
            {
                DestoryEntity(MNetAPI.Client.Entities.Last());
            }
            else
            {
                Debug.LogWarning("No More Entities to Destroy");
            }
        }
        void DestoryEntity(NetworkEntity entity)
        {
            MNetAPI.Client.RequestDestoryEntity(entity);
        }
    }
}