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

using System.Collections.Concurrent;

namespace Backend
{
	public abstract class NetworkTransport
	{
        public abstract bool IsConnected { get; }

        public abstract void Connect(uint context);

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
        public delegate void MessageDelegate(NetworkMessage message);
        public event MessageDelegate OnRecievedMessage;
        void InvokeRecievedMessage(NetworkMessage message)
        {
            OnRecievedMessage?.Invoke(message);
        }

        protected virtual void QueueRecievedMessage(NetworkMessage message)
        {
            InputQueue.Enqueue(Action);

            void Action() => InvokeRecievedMessage(message);
        }
        #endregion

        #region Disconnect
        public delegate void DisconnectDelegate();
        public event DisconnectDelegate OnDisconnect;
        void InvokeDisconnected()
        {
            OnDisconnect?.Invoke();
        }

        protected virtual void QueueDisconnect()
        {
            InputQueue.Enqueue(Action);

            void Action() => InvokeDisconnected();
        }
        #endregion

        public virtual void Poll()
        {
            while (InputQueue.TryDequeue(out var action))
                action();
        }

        public abstract void Send(byte[] raw);

        public abstract void Close();

        public NetworkTransport()
        {
            InputQueue = new ConcurrentQueue<Action>();
        }
    }
}