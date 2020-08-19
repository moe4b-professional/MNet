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
            NetworkClient.Configure(address);

            NetworkClient.RestAPI.Room.OnCreated += RoomCreatedCallback;
            NetworkClient.RestAPI.Lobby.OnInfo += LobbyInfoCallback;
        }

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.F))
            {
                if (Application.isEditor)
                    NetworkClient.RestAPI.Room.Create("Moe4B's Game Room", 4);
                else
                    NetworkClient.RestAPI.Lobby.Info();
            }
        }

        void LobbyInfoCallback(LobbyInfo lobby)
        {
            var room = lobby.Rooms.FirstOrDefault();

            Debug.Log("Lobby Size: " + lobby.Size);

            if (room == null) return;

            NetworkClient.Room.Join(room.ID);
        }

        void RoomCreatedCallback(RoomInfo room)
        {
            Debug.Log("Created Room: " + room.ID);

            NetworkClient.Room.Join(room.ID);
        }
    }

    public partial class SampleObject : INetSerializable
    {
        public int number;

        public string text;

        public string[] array;

        public List<string> list;

        public KeyValuePair<string, string> keyvalue;

        public Dictionary<string, string> dictionary;

        public DateTime date;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(number);
            writer.Write(text);
            writer.Write(array);
            writer.Write(list);
            writer.Write(keyvalue);
            writer.Write(dictionary);
            writer.Write(date);
        }

        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out number);
            reader.Read(out text);
            reader.Read(out array);
            reader.Read(out list);
            reader.Read(out keyvalue);
            reader.Read(out dictionary);
            reader.Read(out date);
        }

        public SampleObject()
        {

        }

        public static void Test()
        {
            byte[] binary;

            {
                var sample = new SampleObject()
                {
                    number = 42,
                    text = "Hello Serializer",
                    array = new string[]
                    {
                        "Welcome",
                        "To",
                        "Roayal",
                        "Mania"
                    },
                    list = new List<string>()
                    {
                        "The",
                        "Fun",
                        "Is",
                        "Just",
                        "Beginning"
                    },
                    keyvalue = new KeyValuePair<string, string>("One Ring", "Destruction"),
                    dictionary = new Dictionary<string, string>()
                    {
                        { "Name", "Moe4B" },
                        { "Level", "14" },
                    },
                    date = DateTime.Now,
                };

                binary = NetworkSerializer.Serialize(sample);
            }

            {
                var sample = NetworkSerializer.Deserialize<SampleObject>(binary);
            }
        }
    }

    public class Vector3SerializationResolver : PocoNetworkSerializationResolver
    {
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

        public Vector3SerializationResolver() : base(typeof(Vector3)) { }
    }
}