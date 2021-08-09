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

using System.Threading;
using System.Collections.Concurrent;

using Utility = MNet.NetworkTransportUtility;

namespace MNet
{
    public abstract class NetworkTransport
    {
        public abstract bool IsConnected { get; }

        public abstract void Connect(GameServerID server, RoomID room);

        public abstract int CheckMTU(DeliveryMode mode);

        public delegate void ConnectDelegate();
        public event ConnectDelegate OnConnect;
        protected virtual void InvokeConnect()
        {
            OnConnect?.Invoke();
        }

        public delegate void MessageDelegate(NetworkMessage message, DeliveryMode mode);
        public event MessageDelegate OnMessage;
        protected virtual void InvokeMessage(NetworkMessage message, DeliveryMode mode)
        {
            OnMessage?.Invoke(message, mode);
        }

        public delegate void DisconnectDelegate(DisconnectCode code);
        public event DisconnectDelegate OnDisconnect;
        protected virtual void InvokeDisconnect(DisconnectCode code)
        {
            OnDisconnect?.Invoke(code);
        }

        public abstract void Send(ArraySegment<byte> segment, DeliveryMode mode, byte channel);

        protected void InvokeMessages(ArraySegment<byte> segment, DeliveryMode mode)
        {
            foreach (var message in NetworkMessage.Read(segment))
                InvokeMessage(message, mode);
        }

        public abstract void Close();

        public NetworkTransport()
        {
            
        }

        //Static Utility
        public static bool IsSupported(NetworkTransportType transport, RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.WebGLPlayer:
                    return transport == NetworkTransportType.WebSockets;
            }

            return true;
        }
    }
}