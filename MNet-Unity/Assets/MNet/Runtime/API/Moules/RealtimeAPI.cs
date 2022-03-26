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

            internal static class InputPackets
            {
                static Queue<Packet> Incoming;
                internal class Packet
                {
                    public PacketType Type;

                    public ArraySegment<byte> Segment;
                    public DeliveryMode DeliveryMode;
                    public Action Dispose;

                    public DisconnectCode DisconnectCode;

                    public void Connect()
                    {
                        Type = PacketType.Connection;
                    }
                    public void Message(ArraySegment<byte> segment, DeliveryMode deliveryMode, Action dispose)
                    {
                        Type = PacketType.Message;

                        this.Segment = segment;
                        this.DeliveryMode = deliveryMode;
                        this.Dispose = dispose;
                    }
                    public void Disconnect(DisconnectCode code)
                    {
                        Type = PacketType.Disconnection;

                        this.DisconnectCode = code;
                    }
                }
                internal enum PacketType
                {
                    Connection, Message, Disconnection
                }

                static void Release(Packet packet)
                {
                    if (packet.Type == PacketType.Message)
                        packet.Dispose?.Invoke();

                    ObjectPool<Packet>.Return(packet);
                }

                internal static void Configure()
                {
                    Incoming = new Queue<Packet>();

                    OnInitialize += Initialize;
                }

                static void Initialize(NetworkTransport transport)
                {
                    Transport.OnConnect += QueueConnect;
                    Transport.OnMessage += QueueMessage;
                    Transport.OnDisconnect += QueueDisconnect;
                }

                static void QueueConnect()
                {
                    var packet = ObjectPool<Packet>.Lease();
                    packet.Connect();

                    lock(Incoming) Incoming.Enqueue(packet);
                }
                static void QueueMessage(ArraySegment<byte> segment, DeliveryMode mode, Action dispose)
                {
                    var packet = ObjectPool<Packet>.Lease();
                    packet.Message(segment, mode, dispose);

                    lock (Incoming) Incoming.Enqueue(packet);
                }
                static void QueueDisconnect(DisconnectCode code)
                {
                    var packet = ObjectPool<Packet>.Lease();
                    packet.Disconnect(code);

                    lock (Incoming) Incoming.Enqueue(packet);
                }

                internal static void Poll()
                {
                    var count = Incoming.Count;

                    while (true)
                    {
                        Packet packet;

                        lock (Incoming)
                        {
                            if (Incoming.TryDequeue(out packet) == false)
                                break;
                        }

                        switch (packet.Type)
                        {
                            case PacketType.Connection:
                            {
                                InvokeConnect();
                            }
                            break;

                            case PacketType.Message:
                            {
                                InvokeMessage(packet.Segment, packet.DeliveryMode);
                            }
                            break;

                            case PacketType.Disconnection:
                            {
                                InvokeDisconnect(packet.DisconnectCode);
                            }
                            break;
                        }

                        Release(packet);

                        count -= 1;

                        if (count <= 0) break;
                    }
                }

                internal static void Clear()
                {
                    lock (Incoming)
                    {
                        while (Incoming.TryDequeue(out var packet))
                        {
                            Release(packet);
                        }
                    }
                }
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
                Pause.Configure();
                InputPackets.Configure();

                NetworkAPI.OnProcess += Process;

                AppAPI.OnSet += Initialize;

                Application.quitting += ApplicationQuitCallback;
            }

            public delegate void InitializeDelegate(NetworkTransport transport);
            public static event InitializeDelegate OnInitialize;
            static void Initialize(AppConfig app)
            {
                Transport = CreateTransport(app.Transport);
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

                OnInitialize?.Invoke(Transport);
            }

            static void Process()
            {
                if (Transport == null) return;

                if (Pause.Active) return;
                if (Client.Buffer.IsOn) return;

                InputPackets.Poll();
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

                InputPackets.Clear();

                if (OfflineMode)
                    InvokeConnect();
                else
                    Transport.Connect(server, room);
            }

            public delegate void ConnectDelegate();
            public static event ConnectDelegate OnConnect;
            static void InvokeConnect()
            {
                OnConnect?.Invoke();
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

            public delegate void MessageDelegate(ArraySegment<byte> segment, DeliveryMode mode);
            public static event MessageDelegate OnMessage;
            static void InvokeMessage(ArraySegment<byte> segment, DeliveryMode mode)
            {
                OnMessage?.Invoke(segment, mode);
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
                InvokeDisconnect(code);
            }

            public delegate void DisconnectDelegate(DisconnectCode code);
            public static event DisconnectDelegate OnDisconnect;
            static void InvokeDisconnect(DisconnectCode code)
            {
                Stop();

                OnDisconnect?.Invoke(code);
            }
            #endregion

            static void ApplicationQuitCallback()
            {
                Application.quitting -= ApplicationQuitCallback;

                if (IsConnected) Disconnect(DisconnectCode.Normal);
            }

            static void Stop()
            {
                if (OfflineMode)
                {
                    NetworkAPI.OfflineMode.Stop();
                    OfflineMode = false;
                }

                InputPackets.Clear();
            }
        }
    }
}