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

using System.Collections.Concurrent;

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
                Buffer = new Queue<NetworkMessage>();

                InputQueue = new ConcurrentQueue<Action>();

                NetworkAPI.OnProcess += Process;

                Server.OnRemoteConfig += Initialize;

                Application.quitting += ApplicationQuitCallback;
            }

            #region Buffer
            public static Queue<NetworkMessage> Buffer { get; private set; }

            public static bool IsOnBuffer => Buffer.Count > 0;

            public static event Action OnAppliedBuffer;

            public static void AddToBuffer(IList<NetworkMessage> list)
            {
                for (int i = 0; i < list.Count; i++)
                    Buffer.Enqueue(list[i]);
            }
            #endregion

            public static ConcurrentQueue<Action> InputQueue { get; private set; }

            public static bool Pause { get; set; } = false;

            public delegate void InitializeDelegate(NetworkTransport transport);
            public static event InitializeDelegate OnInitialize;
            static void Initialize(RemoteConfig config)
            {
                Server.OnRemoteConfig -= Initialize;

                Transport = CreateTransport(config.Transport);

                Transport.OnConnect += QueueConnect;
                Transport.OnMessage += QueueMessage;
                Transport.OnDisconnect += QueueDisconnect;

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

            static void Process()
            {
                if (Transport == null) return;

                if (Pause) return;

                if (IsOnBuffer)
                    ProcessBuffer();
                else
                    ProcessInput();
            }

            static void ProcessBuffer()
            {
                while (Buffer.Count > 0)
                {
                    var message = Buffer.Peek();

                    MessageCallback(message, DeliveryMode.Reliable);

                    Buffer.Dequeue();

                    if (Pause) break;
                }

                OnAppliedBuffer?.Invoke();
            }

            static void ProcessInput()
            {
                var count = InputQueue.Count;

                for (int i = 0; i < count; i++)
                {
                    if (InputQueue.TryDequeue(out var action) == false) break;

                    action();

                    if (Pause) break;
                    if (IsOnBuffer) break;
                }
            }

            #region Connect
            public delegate void ConnectDelegate();
            public static event ConnectDelegate OnConnect;
            static void ConnectCallback()
            {
                OnConnect?.Invoke();
            }

            static void QueueConnect()
            {
                InputQueue.Enqueue(Action);

                void Action() => ConnectCallback();
            }
            #endregion

            #region Message
            public delegate void MessageDelegate(NetworkMessage message, DeliveryMode mode);
            public static event MessageDelegate OnMessage;
            static void MessageCallback(NetworkMessage message, DeliveryMode mode)
            {
                OnMessage?.Invoke(message, mode);
            }

            static void QueueMessage(NetworkMessage message, DeliveryMode mode)
            {
                InputQueue.Enqueue(Action);

                void Action() => MessageCallback(message, mode);
            }
            #endregion

            #region Disconnect
            public delegate void DisconnectDelegate(DisconnectCode code);
            public static event DisconnectDelegate OnDisconnect;
            static void DisconnectCallback(DisconnectCode code)
            {
                OnDisconnect?.Invoke(code);
            }

            static void QueueDisconnect(DisconnectCode code)
            {
                InputQueue.Enqueue(Action);

                void Action() => DisconnectCallback(code);
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
                Application.quitting -= ApplicationQuitCallback;

                if (IsConnected) Disconnect();
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