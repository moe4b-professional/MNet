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
using System.Net;

namespace MNet
{
    public static partial class NetworkAPI
    {
        public static class Realtime
        {
            public static NetworkTransport Transport { get; private set; }

            public static bool IsConnected => Transport == null ? false : Transport.IsConnected;

            public static void Configure()
            {
                Server.OnRemoteConfig += Initialize;
            }

            public delegate void InitializeDelegate(NetworkTransport transport);
            public static event InitializeDelegate OnInitialize;
            static void Initialize(RemoteConfig config)
            {
                Server.OnRemoteConfig -= Initialize;

                Transport = CreateTransport(config.Transport);

                Transport.OnConnect += ConnectCallback;
                Transport.OnMessage += MessageCallback;
                Transport.OnDisconnect += DisconnectCallback;

                OnInitialize?.Invoke(Transport);
            }

            public static void Connect(GameServerID server, RoomID room)
            {
                if (IsConnected)
                {
                    Debug.LogError("Client Must Be Disconnected Before Reconnecting");
                    return;
                }

                //Socket.OnError += ErrorCallback; //TODO Implement Transport Error Handling

                Transport.Connect(server, room);
            }

            public static bool Send(byte[] raw, DeliveryMode mode)
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
                    return false;
                }

                Transport.Send(raw, mode);
                return true;
            }

            static void Update()
            {
                if (Transport != null) Process();
            }

            public static bool Pause { get; set; } = false;

            static void Process()
            {
                if (Pause) return;

                var count = Transport.InputQueue.Count;

                while(true)
                {
                    if (Pause) break;

                    if (Transport.InputQueue.TryDequeue(out var action) == false) break;

                    action();
                    count -= 1;

                    if (count <= 0) break;
                }
            }

            #region Callbacks
            public delegate void ConnectDelegate();
            public static event ConnectDelegate OnConnect;
            static void ConnectCallback()
            {
                OnConnect?.Invoke();
            }

            public delegate void MessageDelegate(NetworkMessage message, DeliveryMode mode);
            public static event MessageDelegate OnMessage;
            static void MessageCallback(NetworkMessage message, DeliveryMode mode)
            {
                OnMessage?.Invoke(message, mode);
            }

            public delegate void DisconnectDelegate(DisconnectCode code);
            public static event DisconnectDelegate OnDisconnect;
            static void DisconnectCallback(DisconnectCode code)
            {
                OnDisconnect?.Invoke(code);
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
                if (IsConnected == false)
                {
                    Debug.LogWarning("Disconnecting Client When They Aren't Connected, Ignoring");
                    return;
                }

                Transport.Close();
            }

            static void ApplicationQuitCallback()
            {
                if (IsConnected) Disconnect();
            }

            static Realtime()
            {
                NetworkAPI.OnUpdate += Update;

                Application.quitting += ApplicationQuitCallback;
            }

            static NetworkTransport CreateTransport(NetworkTransportType type)
            {
                switch (type)
                {
                    case NetworkTransportType.WebSocketSharp:
                        return new WebSocketTransport();

                    case NetworkTransportType.LiteNetLib:
                        return new LiteNetLibTransport();
                }

                throw new NotImplementedException();
            }
        }
    }
}