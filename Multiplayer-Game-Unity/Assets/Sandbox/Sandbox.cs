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

using Backend;

namespace Game
{
	public class Sandbox : MonoBehaviour
	{
        public string address = "127.0.0.1";

        void Start()
        {
            NetworkAPI.Configure(address);

            NetworkAPI.RestAPI.Room.OnCreated += RoomCreatedCallback;
            NetworkAPI.RestAPI.Lobby.OnInfo += LobbyInfoCallback;

            NetworkAPI.Client.OnReady += ClientReadyCallback;

            if(Application.isMobilePlatform)
            {
                NetworkAPI.RestAPI.Lobby.Info();
            }
        }
        
        void ClientReadyCallback(ReadyClientResponse response)
        {
            var attributes = new AttributesCollection();

            var position = new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
            attributes.Set("Position", position);

            var rotation = Quaternion.LookRotation(position.normalized);
            attributes.Set("Rotation", rotation);

            NetworkAPI.Client.RequestSpawnEntity("Player", attributes);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                var attributes = new AttributesCollection();

                attributes.Set("Scene", "Level");

                NetworkAPI.RestAPI.Room.Create("Moe4B's Game Room", 4, attributes);
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                NetworkAPI.RestAPI.Lobby.Info();
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                NetworkAPI.Client.Disconnect();
            }
        }

        void LobbyInfoCallback(LobbyInfo lobby, RestError error)
        {
            if(error == null)
            {
                var room = lobby.Rooms.FirstOrDefault();

                Debug.Log("Lobby Size: " + lobby.Size);

                if (room == null) return;

                NetworkAPI.Room.Join(room);
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

                NetworkAPI.Room.Join(room);
            }
            else
            {
                Debug.LogError(error);
            }
        }
    }
}