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

namespace MNet
{
    public static partial class NetworkAPI
    {
        public static class Realtime
        {
            public static NetworkTransport Transport { get; private set; }

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

            #region Buffer
            public static bool IsOnBuffer { get; private set; } = false;

            public static event BufferDelegate OnBufferBegin;
            internal static async UniTask ApplyBuffer(IList<NetworkMessage> list)
            {
                if (IsOnBuffer) throw new Exception($"Cannot Apply Multiple Buffers at the Same Time");

                IsOnBuffer = true;
                OnBufferBegin?.Invoke(list);

                for (int i = 0; i < list.Count; i++)
                {
                    while (Pause.Value) await UniTask.WaitWhile(Pause.IsOn);

                    MessageCallback(list[i], DeliveryMode.Reliable);
                }

                IsOnBuffer = false;
                OnBufferEnd?.Invoke(list);
            }
            public static event BufferDelegate OnBufferEnd;

            public delegate void BufferDelegate(IList<NetworkMessage> list);
            #endregion

            public static ConcurrentQueue<Action> InputQueue { get; private set; }

            public static class Pause
            {
                static HashSet<object> locks;

                public static bool Value => locks.Count > 0;

                public static bool IsOn() => Value == true;
                public static bool IsOff() => Value == false;

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
                OfflineMode = false;

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

                if (NetworkAPI.OfflineMode.On)
                {
                    OfflineMode = true;
                    QueueConnect();
                    return;
                }

                Transport.Connect(server, room);
            }

            internal static bool Send(byte[] raw, DeliveryMode mode)
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
                OfflineMode = false;

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
            static void DisconnectCallback(DisconnectCode code)
            {
                Clear();

                InvokeDisconnect(code);
            }

            public delegate void DisconnectDelegate(DisconnectCode code);
            public static event DisconnectDelegate OnDisconnect;
            static void InvokeDisconnect(DisconnectCode code)
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

                if (OfflineMode == false) Transport.Close();

                InvokeDisconnect(DisconnectCode.Normal);
                Clear();
            }

            static void ApplicationQuitCallback()
            {
                Application.quitting -= ApplicationQuitCallback;

                if (IsConnected) Disconnect();
            }

            static NetworkTransport CreateTransport(NetworkTransportType type)
            {
#if UNITY_EDITOR
                var platform = ConvertBuildTarget(EditorUserBuildSettings.activeBuildTarget);
#else
                var platform = Application.platform;
#endif

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

#if UNITY_EDITOR
            static RuntimePlatform ConvertBuildTarget(BuildTarget target)
            {
                switch (target)
                {
                    case BuildTarget.StandaloneOSX:
                        return RuntimePlatform.OSXPlayer;

                    case BuildTarget.StandaloneWindows:
                        return RuntimePlatform.WindowsPlayer;

                    case BuildTarget.iOS:
                        return RuntimePlatform.IPhonePlayer;

                    case BuildTarget.Android:
                        return RuntimePlatform.Android;

                    case BuildTarget.StandaloneWindows64:
                        return RuntimePlatform.WindowsPlayer;

                    case BuildTarget.WebGL:
                        return RuntimePlatform.WebGLPlayer;

                    case BuildTarget.WSAPlayer:
                        return RuntimePlatform.WSAPlayerX64;

                    case BuildTarget.StandaloneLinux64:
                        return RuntimePlatform.LinuxPlayer;

                    case BuildTarget.PS4:
                        return RuntimePlatform.PS4;

                    case BuildTarget.XboxOne:
                        return RuntimePlatform.XboxOne;

                    case BuildTarget.tvOS:
                        return RuntimePlatform.tvOS;

                    case BuildTarget.Switch:
                        return RuntimePlatform.Switch;

                    case BuildTarget.Lumin:
                        return RuntimePlatform.Lumin;

                    case BuildTarget.Stadia:
                        return RuntimePlatform.Stadia;
                }

                throw new Exception($"Invalid Build Target: {target}");
            }
#endif
        }
    }
}