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

        void ClientReadyCallback()
        {
            NetworkAPI.Client.Spawn("Player");
        }

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.F))
            {
                if (Application.isEditor)
                    NetworkAPI.RestAPI.Room.Create("Moe4B's Game Room", 4);
                else
                    NetworkAPI.RestAPI.Lobby.Info();
            }
        }

        void LobbyInfoCallback(LobbyInfo lobby)
        {
            var room = lobby.Rooms.FirstOrDefault();

            Debug.Log("Lobby Size: " + lobby.Size);

            if (room == null) return;

            NetworkAPI.Room.Join(room);
        }

        void RoomCreatedCallback(RoomBasicInfo room)
        {
            Debug.Log("Created Room: " + room.ID);

            NetworkAPI.Room.Join(room);
        }
    }

    public class Vector3SerializationResolver : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type) => type == typeof(Vector3);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = (Vector3)type;

            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            reader.Read(out float x);
            reader.Read(out float y);
            reader.Read(out float z);

            return new Vector3(x, y, z);
        }

        public Vector3SerializationResolver() { }
    }

    public class QuaternionNetworkSerializationResolver : NetworkSerializationResolver
    {
        public override bool CanResolve(Type type) => type == typeof(Quaternion);

        public override void Serialize(NetworkWriter writer, object type)
        {
            var value = (Quaternion)type;

            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        public override object Deserialize(NetworkReader reader, Type type)
        {
            reader.Read(out float x);
            reader.Read(out float y);
            reader.Read(out float z);
            reader.Read(out float w);

            return new Quaternion(x, y, z, w);
        }

        public QuaternionNetworkSerializationResolver() { }
    }
}