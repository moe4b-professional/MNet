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

namespace Backend
{
	public abstract class NetworkTransport
	{
        public abstract bool IsConnected { get; }

        public const int Port = Constants.Server.Game.Realtime.Port;

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

    public abstract class AutoDistributedNetworkTransport : NetworkTransport
    {
        public bool IsRegistered { get; protected set; }

        public RoomID Room { get; protected set; }

        public override void Connect(GameServerID server, RoomID room)
        {
            this.Room = room;

            IsRegistered = false;
        }

        Thread thread;
        void Run()
        {
            isRunning = true;

            while (isRunning) Tick();
        }
        bool isRunning;

        protected abstract void Tick();

        protected virtual void Stop()
        {
            isRunning = false;
        }

        protected virtual void RequestRegister()
        {
            var raw = BitConverter.GetBytes(Room.Value);

            Send(raw);
        }
        protected virtual void RegisterCallback(byte[] raw)
        {
            var code = raw[0];

            IsRegistered = code == 200;

            if (IsRegistered)
                QueueConnect();
            else
                QueueDisconnect();
        }

        protected virtual void ProcessMessage(byte[] raw)
        {
            if (IsRegistered)
            {
                var message = NetworkMessage.Read(raw);

                QueueRecievedMessage(message);
            }
            else
            {
                RegisterCallback(raw);
            }
        }

        void ApplicationQuitCallback()
        {
            Application.quitting -= ApplicationQuitCallback;

            Stop();
        }

        public AutoDistributedNetworkTransport() : base()
        {
            thread = new Thread(Run);
            thread.Start();

            Application.quitting += ApplicationQuitCallback;
        }
    }
}