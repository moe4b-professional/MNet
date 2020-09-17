using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Backend
{
    abstract class NetworkTransport
    {
        public Dictionary<uint, NetworkTransportContext> Contexts { get; protected set; }

        public NetworkTransportContext this[uint code] => Contexts[code];

        public abstract void Start();

        public virtual NetworkTransportContext Register(uint id)
        {
            var context = Create(id);

            Contexts.Add(id, context);

            return context;
        }
        protected abstract NetworkTransportContext Create(uint id);

        public virtual void Unregister(uint id)
        {
            if (Contexts.TryGetValue(id, out var context) == false)
            {
                Log.Error($"No Network Transport Context Registered With {id}");
                return;
            }

            Unregister(context);
        }
        protected virtual void Unregister(NetworkTransportContext context)
        {
            context.Close();

            Contexts.Remove(context.ID);
        }

        public NetworkTransport()
        {
            Contexts = new Dictionary<uint, NetworkTransportContext>();
        }
    }

    abstract class NetworkTransportContext
    {
        public uint ID { get; protected set; }

        public ConcurrentQueue<Action> InputQueue { get; protected set; }

        public AutoKeyCollection<NetworkClientID> Clients { get; protected set; }

        public NetworkClientID ReserveID() => Clients.Reserve();
        public void FreeID(NetworkClientID id) => Clients.Free(id);

        #region Connect
        public delegate void ConnectDelegate(NetworkClientID client);
        public event ConnectDelegate OnConnect;
        void InvokeConnect(NetworkClientID client)
        {
            OnConnect?.Invoke(client);
        }

        protected virtual void QueueConnect(NetworkClientID client)
        {
            InputQueue.Enqueue(Action);

            void Action() => InvokeConnect(client);
        }
        #endregion

        #region Message
        public delegate void MessageDelegate(NetworkClientID client, NetworkMessage message, ArraySegment<byte> raw);
        public event MessageDelegate OnRecievedMessage;
        void InvokeRecievedMessage(NetworkClientID client, NetworkMessage message, ArraySegment<byte> raw)
        {
            OnRecievedMessage?.Invoke(client, message, raw);
        }

        protected virtual void QueueRecievedMessage(NetworkClientID client, NetworkMessage message, ArraySegment<byte> raw)
        {
            InputQueue.Enqueue(Action);

            void Action() => InvokeRecievedMessage(client, message, raw);
        }
        #endregion

        #region Disconnect
        public delegate void DisconnectDelegate(NetworkClientID client);
        public event DisconnectDelegate OnDisconnect;
        void InvokeDisconnect(NetworkClientID client)
        {
            OnDisconnect?.Invoke(client);

            Remove(client);
        }

        protected virtual void QueueDisconnect(NetworkClientID client)
        {
            InputQueue.Enqueue(Action);

            void Action() => InvokeDisconnect(client);
        }
        #endregion

        public virtual void Poll()
        {
            while (InputQueue.TryDequeue(out var action))
                action();
        }

        public abstract void Send(NetworkClientID target, byte[] raw);

        public virtual void Remove(NetworkClientID client)
        {
            FreeID(client);
        }

        public abstract void Close();

        public NetworkTransportContext(uint id)
        {
            this.ID = id;

            Clients = new AutoKeyCollection<NetworkClientID>(NetworkClientID.Increment);

            InputQueue = new ConcurrentQueue<Action>();
        }
    }
}