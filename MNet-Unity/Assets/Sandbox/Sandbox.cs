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
            NetworkAPI.Server.Master.OnInfo += MasterServerInfoCallback;

            NetworkAPI.Lobby.OnInfo += LobbyInfoCallback;
            NetworkAPI.Room.OnCreate += RoomCreateCallback;

            NetworkAPI.Client.Profile = new NetworkClientProfile("Moe4B");
            NetworkAPI.Client.OnReady += ClientReadyCallback;

            NetworkAPI.Server.Master.GetInfo();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                var attributes = new AttributesCollection();

                attributes.Set(0, "Level");

                NetworkAPI.Room.Create("Moe4B's Game Room", 4, attributes);
            }

            if (Input.GetKeyDown(KeyCode.V)) NetworkAPI.Lobby.GetInfo();

            if (Input.GetKeyDown(KeyCode.L)) NetworkAPI.Client.Disconnect();

            if (Input.GetKeyDown(KeyCode.E)) SpawnPlayer();

            if (Input.GetKeyDown(KeyCode.Q)) DestroyEntity();

            if (Input.GetKeyDown(KeyCode.I)) Log.Info(NetworkAPI.Room.Clients.ToPrettyString());
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

                    NetworkAPI.Server.Game.Select(server);
                }
            }
            else
            {
                Debug.LogError(error);
            }
        }

        void LobbyInfoCallback(LobbyInfo lobby, RestError error)
        {
            if (error == null)
            {
                Debug.Log("Lobby Size: " + lobby.Size);

                if(lobby.Size > 0) NetworkAPI.Room.Join(lobby[0]);
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

                NetworkAPI.Room.Join(room);
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

            NetworkAPI.Client.SpawnEntity("Player", attributes);
        }

        void DestroyEntity()
        {
            if (NetworkAPI.Client.Entities.Count > 0)
            {
                DestroyEntity(NetworkAPI.Client.Entities.Last());
            }
            else
            {
                Debug.LogWarning("No More Entities to Destroy");
            }
        }
        void DestroyEntity(NetworkEntity entity)
        {
            NetworkAPI.Client.DestroyEntity(entity);
        }
    }
}