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

using Cysharp.Threading.Tasks;

using MB;

namespace MNet
{
    public static partial class NetworkAPI
    {
        public static class Realtime
        {
            public static NetworkTransport Transport { get; private set; }
            public static bool IsInitialized => Transport != null;

            public static bool IsConnected
            {
                get
                {
                    if (OfflineMode) return true;

                    if (Transport == null)
                        return false;

                    return Transport.IsConnected;
                }
            }

            public static bool OfflineMode { get; private set; }

            public static ConcurrentQueue<Action> InputQueue { get; private set; }

            /// <summary>
            /// Class responsible for applying room buffers for late joining clients
            /// </summary>
            public static class Buffer
            {
                public static bool IsOn { get; private set; } = false;

                public delegate void BufferDelegate(IList<NetworkMessage> list);

                public static event BufferDelegate OnBegin;

                internal static async UniTask Apply(IList<NetworkMessage> list)
                {
                    if (IsOn) throw new Exception($"Cannot Apply Multiple Buffers at the Same Time");

                    IsOn = true;
                    OnBegin?.Invoke(list);

                    for (int i = 0; i < list.Count; i++)
                    {
                        while (Pause.Active) await UniTask.WaitWhile(Pause.IsOn);

                        MessageCallback(list[i], DeliveryMode.ReliableOrdered);
                    }

                    IsOn = false;
                    OnEnd?.Invoke(list);
                }

                public static event BufferDelegate OnEnd;
            }

            /// <summary>
            /// Class responsible for pausing the RealtimeAPI processing when peforming asynchronous operations 
            /// such as loading or unloading a scene
            /// </summary>
            public static class Pause
            {
                static HashSet<object> locks;

                public static bool Active => locks.Count > 0;

                public static bool IsOn() => Active == true;
                public static bool IsOff() => Active == false;

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

                AppAPI.OnSet += Initialize;

                Application.quitting += ApplicationQuitCallback;
            }

            public delegate void InitializeDelegate(NetworkTransport transport);
            public static event InitializeDelegate OnInitialize;
            static void Initialize(AppConfig app)
            {
                Transport = CreateTransport(app.Transport);

                Transport.OnConnect += QueueConnect;
                Transport.OnMessage += QueueMessage;
                Transport.OnDisconnect += QueueDisconnect;

                OnInitialize?.Invoke(Transport);
            }

            static void Process()
            {
                if (Transport == null) return;

                if (Pause.Active) return;
                if (Buffer.IsOn) return;

                var count = InputQueue.Count;

                for (int i = 0; i < count; i++)
                {
                    if (Pause.Active) return;
                    if (Buffer.IsOn) return;

                    if (InputQueue.TryDequeue(out var action) == false) return;

                    action();
                }
            }

            static void Stop()
            {
                if (OfflineMode)
                {
                    NetworkAPI.OfflineMode.Stop();
                    OfflineMode = false;
                }

                ///Manually reset the InputQueue just to ensure that no commands are executed after this method is invoked
                InputQueue = new ConcurrentQueue<Action>();
            }

            #region Connect
            public static void Connect(GameServerID server, RoomID room)
            {
                if (IsInitialized == false)
                {
                    Debug.LogError($"Cannot Connect Because Realtime API NetworkTransport is not Initialized, " +
                        $"Did you not Retrieve the Master Server Scheme Yet?");
                    return;
                }

                if (IsConnected)
                {
                    Debug.LogError("Client Must Be Disconnected Before Reconnecting");
                    return;
                }

                OfflineMode = NetworkAPI.OfflineMode.On;

                if (OfflineMode)
                    QueueConnect();
                else
                    Transport.Connect(server, room);
            }

            public delegate void ConnectDelegate();
            public static event ConnectDelegate OnConnect;
            static void ConnectCallback()
            {
                OnConnect?.Invoke();
            }

            static void QueueConnect()
            {
                InputQueue.Enqueue(Action);

                static void Action() => ConnectCallback();
            }
            #endregion

            #region Message
            public static bool Send(ArraySegment<byte> segment, DeliveryMode mode, byte channel)
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

                Transport.Send(segment, mode, channel);
                return true;
            }

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
            public static void Disconnect(DisconnectCode code)
            {
                if (IsConnected == false)
                {
                    Debug.LogWarning("Disconnecting Client When They Aren't Connected, Ignoring");
                    return;
                }

                if (OfflineMode == false) Transport.Disconnect(code);

                ///Manually invoke callback to make all Disconnect() invokes synchronous,
                ///we ensure synchronicity by clearing the InputQueue within the callback
                DisconnectCallback(code);
            }

            public delegate void DisconnectDelegate(DisconnectCode code);
            public static event DisconnectDelegate OnDisconnect;
            static void DisconnectCallback(DisconnectCode code)
            {
                Stop();

                OnDisconnect?.Invoke(code);
            }

            static void QueueDisconnect(DisconnectCode code)
            {
                InputQueue.Enqueue(Action);

                void Action() => DisconnectCallback(code);
            }
            #endregion

            static void ApplicationQuitCallback()
            {
                Application.quitting -= ApplicationQuitCallback;

                if (IsConnected) Disconnect(DisconnectCode.Normal);
            }

            static NetworkTransport CreateTransport(NetworkTransportType type)
            {
                var platform = MUtility.CheckPlatform();

                if (NetworkTransport.IsSupported(type, platform) == false)
                    throw new Exception($"{type} Transport not Supported on {platform}");

                switch (type)
                {
                    case NetworkTransportType.WebSockets:
                        return new WebSocketTransport();

                    case NetworkTransportType.LiteNetLib:
                        return new LiteNetLibTransport();
                }

                throw new NotImplementedException();
            }
        }
    }
}