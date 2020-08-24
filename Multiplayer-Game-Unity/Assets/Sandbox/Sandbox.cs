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

using Game.Shared;

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
            if(Input.GetKeyDown(KeyCode.F))
            {
                if (Application.isEditor)
                {
                    var attributes = new AttributesCollection();

                    attributes.Set("Scene", "Level");

                    NetworkAPI.RestAPI.Room.Create("Moe4B's Game Room", 4, attributes);
                }
                else
                {
                    NetworkAPI.RestAPI.Lobby.Info();
                }
            }
        }

        void LobbyInfoCallback(LobbyInfo lobby)
        {
            var room = lobby.Rooms.FirstOrDefault();

            Debug.Log("Lobby Size: " + lobby.Size);

            if (room.Attributes == null)
                Debug.LogError("Null Room Attributes");
            else
            {
                foreach (var key in room.Attributes.Keys)
                    Debug.Log(room.Attributes[key]);
            }

            if (room == null) return;

            NetworkAPI.Room.Join(room);
        }

        void RoomCreatedCallback(RoomBasicInfo room)
        {
            Debug.Log("Created Room " + room.ID);

            if (room.Attributes == null)
                Debug.LogError("Null Room Attributes");
            else
            {
                foreach (var key in room.Attributes.Keys)
                    Debug.Log(room.Attributes[key]);
            }

            NetworkAPI.Room.Join(room);
        }
    }
}