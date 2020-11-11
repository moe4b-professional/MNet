﻿using System;
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

        public ConcurrentQueue<Action> InputQueue { get; protected set; }

        #region Connect
        public delegate void ConnectDelegate();
        public event ConnectDelegate OnConnect;
        void InvokeConnect()
        {
            OnConnect?.Invoke();
        }

        protected virtual void QueueConnect()
        {
            InputQueue.Enqueue(Action);

            void Action() => InvokeConnect();
        }
        #endregion

        #region Message
        public delegate void MessageDelegate(NetworkMessage message, DeliveryMode mode);
        public event MessageDelegate OnRecievedMessage;
        void InvokeRecievedMessage(NetworkMessage message, DeliveryMode mode)
        {
            OnRecievedMessage?.Invoke(message, mode);
        }

        protected virtual void QueueRecievedMessage(NetworkMessage message, DeliveryMode mode)
        {
            InputQueue.Enqueue(Action);

            void Action() => InvokeRecievedMessage(message, mode);
        }
        #endregion

        #region Disconnect
        public delegate void DisconnectDelegate(DisconnectCode code);
        public event DisconnectDelegate OnDisconnect;
        void InvokeDisconnected(DisconnectCode code)
        {
            OnDisconnect?.Invoke(code);
        }

        protected virtual void QueueDisconnect(DisconnectCode code)
        {
            InputQueue.Enqueue(Action);

            void Action() => InvokeDisconnected(code);
        }
        #endregion

        public abstract void Send(byte[] raw, DeliveryMode mode);

        public abstract void Close();

        public NetworkTransport()
        {
            InputQueue = new ConcurrentQueue<Action>();
        }
    }
}