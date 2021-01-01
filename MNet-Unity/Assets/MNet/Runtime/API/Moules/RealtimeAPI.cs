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
using System.Threading.Tasks;

namespace MNet
{
    public static partial class NetworkAPI
    {
        public static class Realtime
        {
            public static NetworkTransport Transport { get; private set; }

            public static bool IsConnected => Transport == null ? false : Transport.IsConnected;

            #region Buffer
            public static bool IsOnBuffer { get; private set; } = false;

            public static event Action OnBufferBegin;

            internal static async void ApplyBuffer(IList<NetworkMessage> list)
            {
                if (IsOnBuffer) throw new Exception($"Cannot Apply Multiple Buffers at the Same Time");

                IsOnBuffer = true;
                OnBufferBegin?.Invoke();

                for (int i = 0; i < list.Count; i++)
                {
                    while (Pause.Value) await Task.Delay(1);

                    MessageCallback(list[i], DeliveryMode.Reliable);
                }

                IsOnBuffer = false;
                OnBufferEnd?.Invoke();
            }

            public static event Action OnBufferEnd;
            #endregion

            public static ConcurrentQueue<Action> InputQueue { get; private set; }

            public static class Pause
            {
                static HashSet<object> locks;

                public static bool Value => locks.Count > 0;

                internal static void Configure()
                {
                    locks = new HashSet<object>();
                }

                public static event Action OnBegin;
                static void Begin()
                {
                    OnBegin?.Invoke();
                }

                public static object AddLock()
                {
                    var instance = new object();

                    return AddLock(instance);
                }
                public static object AddLock(object instance)
                {
                    var delta = locks.Count;

                    locks.Add(instance);

                    if (delta == 0) Begin();

                    return instance;
                }

                public static void RemoveLock(object instance)
                {
                    if (locks.Remove(instance) == false) return;

                    if (locks.Count == 0) End();
                }

                public static event Action OnEnd;
                static void End()
                {
                    OnEnd.Invoke();
                }
            }

            internal static void Configure()
            {
                InputQueue = new ConcurrentQueue<Action>();

                Pause.Configure();

                NetworkAPI.OnProcess += Process;

                Server.OnRemoteConfig += Initialize;

                Application.quitting += ApplicationQuitCallback;
            }

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

                if (Pause.Value) return;
                if (IsOnBuffer) return;

                var count = InputQueue.Count;

                for (int i = 0; i < count; i++)
                {
                    if (Pause.Value) return;
                    if (IsOnBuffer) return;

                    if (InputQueue.TryDequeue(out var action) == false) return;

                    action();
                }
            }

            internal static void Clear()
            {
                InputQueue = new ConcurrentQueue<Action>();
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
                Clear();

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

                Clear();

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