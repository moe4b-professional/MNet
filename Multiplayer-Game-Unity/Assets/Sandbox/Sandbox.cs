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

using Game.Fixed;

namespace Game
{
	public class Sandbox : MonoBehaviour
	{
        WebSocket websocket;

        void Start()
        {
            Debug.Log("Start");

            ConnectWebSocket();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
                SendRestRequest();
        }

        void SendRestRequest()
        {
            Call<ListRoomsMessage>("localhost", "GET", Constants.RestAPI.Requests.ListRooms, Callback);

            void Callback(ListRoomsMessage message)
            {
                foreach (var room in message.list)
                    Debug.Log(room);
            }
        }

        public delegate void ResponseDelegate<TMessage>(TMessage message) where TMessage : NetworkMessage;
        void Call<TResponse>(string address, string method, string path, ResponseDelegate<TResponse> callback)
            where TResponse : NetworkMessage
        {
            StartCoroutine(Procedure());
            IEnumerator Procedure()
            {
                var url = address + ":" + Constants.RestAPI.Port + path;

                var request = new UnityWebRequest(url, method);

                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                var message = NetworkMessage.Deserialize<TResponse>(request.downloadHandler.data);

                callback(message);
            }
        }

        void ConnectWebSocket()
        {
            websocket = new WebSocket($"ws://localhost:{Constants.WebSockeAPI.Port}/");

            websocket.OnError += ErrorCallback;
            void ErrorCallback(object sender, WebSocketSharp.ErrorEventArgs args)
            {
                Debug.LogWarning($"WebSocket Error: {args.Message}");
            }

            websocket.OnOpen += OpenCallback;
            void OpenCallback(object sender, EventArgs args)
            {
                Debug.Log("Websocket Opened");

                websocket.Send("Hello Server");
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
                websocket.Close();
            }

            websocket.Connect();
        }
    }
}