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

using System.Runtime.InteropServices;

using Backend;

using System.Threading;

namespace Game
{
	public static class NativeWebSocket
	{
        [RuntimeInitializeOnLoadMethod()]
        static void OnLoad()
        {
            var sandbox = Object.FindObjectOfType<Sandbox>();

            sandbox.StartCoroutine(Procedure());
        }

        static IEnumerator Procedure()
        {
            Debug.Log("Start Socket Procedure");

            var id = WS_Connect($"ws://127.0.0.1:{Constants.WebSocketAPI.Port}/0");

            Debug.Log("Socket ID: " + id);

            bool isConnected() => WS_CheckState(id) == 1;

            yield return new WaitUntil(isConnected);

            var payload = new RegisterClientRequest(new NetworkClientProfile("Moe4B"));
            var message = NetworkMessage.Write(payload);
            var binary = NetworkSerializer.Serialize(message);

            WS_Send(id, binary);

            Debug.Log("Data Sent");
        }

        [DllImport("__Internal")]
        static extern int WS_Connect(string path);

        [DllImport("__Internal")]
        static extern int WS_CheckState(int id);

        [DllImport("__Internal")]
        static extern int WS_Send(int id, byte[] buffer);

        [DllImport("__Internal")]
        static extern int WS_Disconnect(int id, int code, string reason); 
    }
}