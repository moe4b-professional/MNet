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

namespace Backend
{
	public static partial class NetworkAPI
	{
        public static class RealtimeAPI
        {
            public const int Port = Constants.GameServer.Realtime.Port;

            public static NetworkTransport Transport { get; private set; }

            public static bool IsConnected => Transport == null ? false : Transport.IsConnected;

            public static void Configure()
            {
                Transport = new WebSocketTransport(Address, Port);
                //Transport = new LiteNetLibTransport(Address, Port);

                Transport.OnConnect += ConnectCallback;
                Transport.OnRecievedMessage += MessageCallback;
                Transport.OnDisconnect += DisconnectCallback;
            }

            public static void Connect(uint context)
            {
                if (IsConnected)
                {
                    Debug.LogError("Client Must Be Disconnected Before Reconnecting");
                    return;
                }

                //Socket.OnError += ErrorCallback; //TODO Implement Transport Error Handling

                Transport.Connect(context);
            }

            public static void Send(byte[] raw)
            {
                if (IsConnected == false)
                {
                    ///Sending the client back with a gentle slap on the butt
                    ///because the client disconnection can occur at any time
                    ///and the DisconnectCallback gets called on a worker thread
                    ///so the client's connected state can change literally at anytime
                    ///and the client's code can find themselves rightfully checking connection state
                    ///and sending data after state validation but then the client
                    ///gets disconnected before the Send method is called
                    ///which will result in an exception
                    Debug.LogWarning("Cannot Send Data When Client Isn't Connected");
                    return;
                }

                Transport.Send(raw);
            }

            static void Update()
            {
                if (Transport != null) Transport.Poll();
            }

            #region Callbacks
            public delegate void ConnectDelegate();
            public static event ConnectDelegate OnConnect;
            static void ConnectCallback()
            {
                OnConnect?.Invoke();
            }

            public delegate void MessageDelegate(NetworkMessage message);
            public static event MessageDelegate OnMessage;
            static void MessageCallback(NetworkMessage message)
            {
                OnMessage?.Invoke(message);
            }

            public delegate void CloseDelegate();
            public static event CloseDelegate OnDisconnect;
            static void DisconnectCallback()
            {
                OnDisconnect?.Invoke();
            }

            public delegate void ErrorDelegate();
            public static event ErrorDelegate OnError;
            static void ErrorCallback()
            {
                OnError?.Invoke();
            }
            #endregion

            public static void Disconnect()
            {
                if (IsConnected == false) return;

                Transport.Close();
            }

            static void ApplicationQuitCallback()
            {
                if (IsConnected) Disconnect();
            }

            static RealtimeAPI()
            {
                NetworkAPI.OnUpdate += Update;

                Application.quitting += ApplicationQuitCallback;
            }
        }
    }
}