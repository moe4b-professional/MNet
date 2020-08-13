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

namespace Game
{
	public class Sandbox : MonoBehaviour
	{
        WebSocket websocket;

        void Start()
        {
            ConnectWebSocket();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
                SendRestRequest();
        }

        void SendRestRequest()
        {
            StartCoroutine(Procedure());

            IEnumerator Procedure()
            {
                var request = UnityWebRequest.Get($"localhost:{Constants.RestAPI.Port}/{Constants.RestAPI.Requests.ListMatches}");

                yield return request.SendWebRequest();

                if (request.isDone)
                {
                    Debug.Log(request.responseCode);

                    Debug.Log(request.downloadHandler.text);
                }
            }
        }

        void ConnectWebSocket()
        {
            websocket = new WebSocket($"ws://localhost{Constants.WebSockeAPI.Port}");

            websocket.OnError += ErrorCallback;
            void ErrorCallback(object sender, WebSocketSharp.ErrorEventArgs args)
            {
                Debug.LogWarning($"WebSocket Error: {args.Message}");
            }

            websocket.OnOpen += OpenCallback;
            void OpenCallback(object sender, EventArgs args)
            {
                Debug.Log("Websocket Open");

                websocket.Send("Hello Server");
            }

            websocket.OnMessage += MessageCallback;
            void MessageCallback(object sender, MessageEventArgs args)
            {
                Debug.Log($"WebSocket API: {args.Data}");
            }

            websocket.Connect();
        }
    }
}