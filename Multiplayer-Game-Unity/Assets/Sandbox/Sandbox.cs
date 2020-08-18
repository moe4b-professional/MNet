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

        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {
            NetworkMessagePayload.ValidateAll();
        }

        void Start()
        {
            Debug.Log("Start");

            GetPlayerInfo();
            ListRooms();
        }

        void Update()
        {
            
        }

        void GetPlayerInfo()
        {
            Call<NetworkMessage>(address, "GET", "/PlayerInfo", Callback);
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
            Call<NetworkMessage>(address, "GET", Constants.RestAPI.Requests.ListRooms, Callback);

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
        public delegate void ResponseDelegate(NetworkMessage message, RestError error);

        void Call<TResponse>(string address, string method, string path, ResponseDelegate callback)
        {
            StartCoroutine(Procedure());

            IEnumerator Procedure()
            {
                var url = "http://" + address + ":" + Constants.RestAPI.Port + path;

                var request = new UnityWebRequest(url, method);

                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if(request.isHttpError || request.isNetworkError)
                {
                    var error = new RestError(request);

                    callback(null, error);
                }
                else
                {
                    var message = NetworkMessage.Read(request.downloadHandler.data);

                    callback(message, null);
                }
            }
        }

        public class RestError
        {
            public long Code { get; protected set; }

            public string Message { get; protected set; }

            public RestError(long code, string message)
            {
                this.Code = code;
                this.Message = message;
            }

            public RestError(UnityWebRequest request) : this(request.responseCode, request.error) { }
        }
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