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

using WebSocketSharp;

using Backend;

namespace Game
{
	public class Sandbox : MonoBehaviour
	{
        public string address = "127.0.0.1";

        GameServerID serverID;

        void Start()
        {
            NetworkAPI.Configure(address);

            serverID = GameServerID.Parse(address);

            NetworkAPI.MasterServer.OnInfo += MasterServerInfoCallback;

            NetworkAPI.Lobby.OnInfo += LobbyInfoCallback;
            NetworkAPI.Room.OnCreated += RoomCreatedCallback;

            NetworkAPI.Client.Profile = new NetworkClientProfile("Moe4B");
            NetworkAPI.Client.OnReady += ClientReadyCallback;

            NetworkAPI.MasterServer.Info();

            if (Application.isMobilePlatform)
            {
                NetworkAPI.Lobby.Info(serverID);
            }
        }

        void MasterServerInfoCallback(MasterServerInfoPayload info, RestError error)
        {
            if(error == null)
            {
                Debug.Log($"Game Servers Count: {info.Servers.Length}");

                foreach (var server in info.Servers) Debug.Log($"Game Server: {server}");
            }
            else
            {
                Debug.LogError(error);
            }
        }

        void ClientReadyCallback(ReadyClientResponse response)
        {
            SpawnPlayer();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                var attributes = new AttributesCollection();

                attributes.Set(0, "Level");

                NetworkAPI.Room.Create(serverID, "Moe4B's Game Room", 4, attributes);
            }

            if (Input.GetKeyDown(KeyCode.V)) NetworkAPI.Lobby.Info(serverID);

            if (Input.GetKeyDown(KeyCode.L)) NetworkAPI.Client.Disconnect();

            if (Input.GetKeyDown(KeyCode.E)) SpawnPlayer();

            if (Input.GetKeyDown(KeyCode.Q)) DestroyEntity();
        }

        void LobbyInfoCallback(LobbyInfo lobby, RestError error)
        {
            if(error == null)
            {
                Debug.Log("Lobby Size: " + lobby.Size);

                var room = lobby.Rooms.FirstOrDefault();

                if (room == null) return;

                NetworkAPI.Room.Join(lobby.Server.ID, room);
            }
            else
            {
                Debug.LogError(error);
            }
        }

        void RoomCreatedCallback(RoomBasicInfo room, RestError error)
        {
            if (error == null)
            {
                Debug.Log("Created Room " + room.ID);

                NetworkAPI.Room.Join(serverID, room);
            }
            else
            {
                Debug.LogError(error);
            }
        }

        void SpawnPlayer()
        {
            var attributes = new AttributesCollection();

            var position = new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
            var rotation = Quaternion.LookRotation(position.normalized);

            Player.Write(ref attributes, position, rotation);

            NetworkAPI.Client.RequestSpawnEntity("Player", attributes);
        }

        void DestroyEntity()
        {
            if (NetworkAPI.Client.Entities.Count > 0)
            {
                DestoryEntity(NetworkAPI.Client.Entities.Last());
            }
            else
            {
                Debug.LogWarning("No More Entities to Destroy");
            }
        }
        void DestoryEntity(NetworkEntity entity)
        {
            NetworkAPI.Client.RequestDestoryEntity(entity);
        }
    }
}