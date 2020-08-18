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
        public string address = "localhost";

        void Start()
        {
            Debug.Log("Start");

            GetPlayerInfo();
            ListRooms();

            TryRPC();
        }

        void TryRPC()
        {
            var bind = RPCBind.Parse(this, nameof(RpcCallback));

            byte[] data;

            {
                var payload = RPCPayload.Write("Target", 42, "Hello RPC!", DateTime.UtcNow);

                var message = NetworkMessage.Write(payload);

                data = NetworkSerializer.Serialize(message);

                Debug.Log("RPC Payload Size: " + data.Length);
            }

            {
                var message = NetworkMessage.Read(data);

                var payload = message.Read<RPCPayload>();

                bind.Invoke(payload);
            }
        }
        void RpcCallback(int a, string b, DateTime time)
        {
            Debug.Log($"RPC Callback: {a}, {b}, {time}");
        }

        void Update()
        {
            
        }

        void GetPlayerInfo()
        {
            RestRequest.GET(address, "GET", "/PlayerInfo", Callback, false);
            void Callback(NetworkMessage message, RestError error)
            {
                if (error == null)
                {
                    var payload = message.Read<PlayerInfoPayload>();

                    foreach (var pair in payload.Dictionary)
                    {
                        Debug.Log(pair.Key + " : " + pair.Value);
                    }
                }
                else
                {
                    Debug.LogError("Error Getting Player Info, Message: " + error.Message);
                }
            }
        }
        void ListRooms()
        {
            RestRequest.GET(address, "GET", Constants.RestAPI.Requests.ListRooms, Callback, false);

            void Callback(NetworkMessage message, RestError error)
            {
                if(error == null)
                {
                    var payload = message.Read<ListRoomsPayload>();

                    foreach (var room in payload.List)
                    {
                        Debug.Log("Room :" + room);
                        ConnectWebSocket("/" + room.ID);
                    }
                }
                else
                {
                    Debug.LogError("Error Listing Rooms, Message: " + error.Message);
                }
            }
        }

        #region REST
        
        #endregion

        #region WebSocket
        void ConnectWebSocket(string path)
        {
            var websocket = new WebSocket($"ws://{address}:{Constants.WebSocketAPI.Port}{path}");

            websocket.OnError += ErrorCallback;
            void ErrorCallback(object sender, WebSocketSharp.ErrorEventArgs args)
            {
                Debug.LogWarning($"WebSocket Error: {args.Message}");
            }

            websocket.OnOpen += OpenCallback;
            void OpenCallback(object sender, EventArgs args)
            {
                Debug.Log("Websocket Opened");

                websocket.Send("Hello Room");
            }

            websocket.OnMessage += MessageCallback;
            void MessageCallback(object sender, MessageEventArgs args)
            {
                Debug.Log($"WebSocket API: {args.Data}");
            }

            websocket.OnClose += CloseCallback;
            void CloseCallback(object sender, CloseEventArgs e)
            {
                Debug.Log($"WebSocket Closed");
            }

            Application.quitting += QuitCallback;
            void QuitCallback()
            {
                if(websocket.IsAlive) websocket.Close();
            }

            websocket.ConnectAsync();
        }
        #endregion
    }

    public class NetworkBehaviour
    {

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
}