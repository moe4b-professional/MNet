using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

using Utility = MNet.NetworkTransportUtility;

namespace MNet
{
    #region Transport
    public interface INetworkTransport
    {
        void Start();

        INetworkTransportContext Register(uint id);
        void Unregister(uint id);
    }

    abstract class NetworkTransport<TTransport, TContext, TClient, TConnection, TIID> : INetworkTransport
        where TTransport : NetworkTransport<TTransport, TContext, TClient, TConnection, TIID>
        where TContext : NetworkTransportContext<TTransport, TContext, TClient, TConnection, TIID>
        where TClient : NetworkTransportClient<TContext, TConnection, TIID>
    {
        public ConcurrentDictionary<uint, TContext> Contexts { get; protected set; }

        public TContext this[uint code] => Contexts[code];

        public abstract void Start();

        public virtual TContext Register(uint id)
        {
            var context = Create(id);

            Contexts.TryAdd(id, context);

            return context;
        }
        protected abstract TContext Create(uint id);

        INetworkTransportContext INetworkTransport.Register(uint id) => Register(id);
        public virtual void Unregister(uint id)
        {
            if (Contexts.TryGetValue(id, out var context) == false)
            {
                Log.Error($"No Network Transport Context Registered With {id}");
                return;
            }

            Unregister(context);
        }
        protected virtual bool Unregister(TContext context)
        {
            context.Close();

            return Contexts.TryRemove(context.ID);
        }

        public NetworkTransport()
        {
            Contexts = new ConcurrentDictionary<uint, TContext>();

            Log.Info($"Configuring {GetType().Name}");
        }
    }
    #endregion

    #region Context
    public interface INetworkTransportContext
    {
        void Poll();

        public event NetworkTransportConnectDelegate OnConnect;
        public event NetworkTransportMessageDelegate OnMessage;
        public event NetworkTransportDisconnectDelegate OnDisconnect;

        void Send(NetworkClientID target, byte[] raw, DeliveryMode mode);

        void Broadcast(ICollection<NetworkClientID> targets, byte[] raw, DeliveryMode mode);
        void Broadcast(byte[] raw, DeliveryMode mode);

        void Disconnect(NetworkClientID clientID, DisconnectCode code);
    }

    abstract class NetworkTransportContext<TTransport, TContext, TClient, TConnection, TIID> : INetworkTransportContext
        where TContext : NetworkTransportContext<TTransport, TContext, TClient, TConnection, TIID>
        where TClient : NetworkTransportClient<TContext, TConnection, TIID>
    {
        public TTransport Transport { get; protected set; }

        public uint ID { get; protected set; }

        public ConcurrentQueue<Action> InputQueue { get; protected set; }

        public AutoKeyCollection<NetworkClientID> ClientIDs { get; protected set; }

        public NetworkClientID ReserveClientID()
        {
            lock (ClientIDs) return ClientIDs.Reserve();
        }
        public bool FreeClientID(NetworkClientID id)
        {
            lock (ClientIDs) return ClientIDs.Free(id);
        }

        public ConcurrentDictionary<NetworkClientID, TClient> Clients { get; protected set; }
        public ConcurrentDictionary<TConnection, TClient> Connections { get; protected set; }

        #region Connect
        public event NetworkTransportConnectDelegate OnConnect;
        void InvokeConnect(TClient client)
        {
            OnConnect?.Invoke(client.ClientID);
        }

        protected virtual void QueueConnect(TClient client)
        {
            InputQueue.Enqueue(Action);

            void Action() => InvokeConnect(client);
        }
        #endregion

        #region Message
        public event NetworkTransportMessageDelegate OnMessage;
        void InvokeMessage(TClient client, NetworkMessage message, ArraySegment<byte> raw, DeliveryMode mode)
        {
            OnMessage?.Invoke(client.ClientID, message, raw, mode);
        }

        protected virtual void QueueMessage(TClient client, NetworkMessage message, ArraySegment<byte> raw, DeliveryMode mode)
        {
            InputQueue.Enqueue(Action);

            void Action() => InvokeMessage(client, message, raw, mode);
        }
        #endregion

        #region Disconnect
        public event NetworkTransportDisconnectDelegate OnDisconnect;
        void InvokeDisconnect(TClient client)
        {
            OnDisconnect?.Invoke(client.ClientID);

            RemoveClient(client);
        }

        protected virtual void QueueDisconnect(TClient client)
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

        #region Register & Add Client
        public virtual TClient RegisterClient(TConnection connection)
        {
            var id = ReserveClientID();

            var client = CreateClient(id, connection);

            AddClient(id, connection, client);

            QueueConnect(client);

            return client;
        }

        void AddClient(NetworkClientID id, TConnection connection, TClient client)
        {
            Clients.TryAdd(id, client);
            Connections.TryAdd(connection, client);
        }
        #endregion

        #region Unregister & Remove Client
        public virtual void UnregisterClient(TConnection connection)
        {
            if (Connections.TryGetValue(connection, out var client) == false)
            {
                Log.Warning($"Trying to Unregister Client with Connection '{connection}' but said Connection was not Registered with Connections Dictionary");
                return;
            }

            UnregisterClient(client);
        }
        public virtual void UnregisterClient(TClient client) => QueueDisconnect(client);

        void RemoveClient(TClient client)
        {
            Clients.TryRemove(client.ClientID);
            Connections.TryRemove(client.Connection);

            FreeClientID(client.ClientID);

            DestroyClient(client);
        }
        #endregion

        #region RegisterMessage
        public virtual void RegisterMessage(TConnection connection, byte[] raw, DeliveryMode mode)
        {
            if (Connections.TryGetValue(connection, out var client) == false)
            {
                Log.Warning($"Trying to Register Message from Connection: {connection} but said Connection was not Registered with Connections Dictionary");
                return;
            }

            RegisterMessage(client, raw, mode);
        }

        public virtual void RegisterMessage(TClient sender, byte[] raw, DeliveryMode mode)
        {
            var message = NetworkMessage.Read(raw);

            QueueMessage(sender, message, raw, mode);
        }
        #endregion

        protected abstract TClient CreateClient(NetworkClientID clientID, TConnection connection);
        protected virtual void DestroyClient(TClient client) { }

        #region Send
        public void Send(NetworkClientID target, byte[] raw, DeliveryMode mode)
        {
            if (Clients.TryGetValue(target, out var client) == false)
            {
                Log.Warning($"No Transport Client Registered With ID: {target}");
                return;
            }

            Send(client, raw, mode);
        }

        public abstract void Send(TClient target, byte[] raw, DeliveryMode mode);
        #endregion

        #region Broadcast
        public virtual void Broadcast(byte[] raw, DeliveryMode mode) => Broadcast(Clients.Values, raw, mode);

        public virtual void Broadcast(ICollection<NetworkClientID> targets, byte[] raw, DeliveryMode mode)
        {
            var collection = GetClientsFrom(targets);

            Broadcast(collection, raw, mode);
        }

        public virtual void Broadcast(ICollection<TClient> targets, byte[] raw, DeliveryMode mode)
        {
            foreach (var target in targets)
            {
                if (target == null) continue;

                Send(target, raw, mode);
            }
        }
        #endregion

        public TClient[] GetClientsFrom(ICollection<NetworkClientID> targets)
        {
            var collection = new TClient[targets.Count];

            var index = 0;

            foreach (var target in targets)
            {
                if (Clients.TryGetValue(target, out collection[index]) == false)
                    Log.Warning($"No Transport Client Registered With ID: {target}");

                index += 1;
            }

            return collection;
        }

        #region Disconnect
        public virtual void Disconnect(TClient client) => Disconnect(client, DisconnectCode.Normal);
        public abstract void Disconnect(TClient client, DisconnectCode code);

        public virtual void Disconnect(NetworkClientID clientID) => Disconnect(clientID, DisconnectCode.Normal);
        public virtual void Disconnect(NetworkClientID clientID, DisconnectCode code)
        {
            if (Clients.TryGetValue(clientID, out var client) == false)
            {
                Log.Warning($"No Transport Client Registered With ID: {clientID}");
                return;
            }

            Disconnect(client, code);
        }
        #endregion

        public virtual void Close()
        {

        }

        public NetworkTransportContext(TTransport transport, uint id)
        {
            this.Transport = transport;
            this.ID = id;

            ClientIDs = new AutoKeyCollection<NetworkClientID>(NetworkClientID.Increment);

            Clients = new ConcurrentDictionary<NetworkClientID, TClient>();
            Connections = new ConcurrentDictionary<TConnection, TClient>();

            InputQueue = new ConcurrentQueue<Action>();
        }
    }
    #endregion

    abstract class NetworkTransportClient<TContext, TConnection, TIID>
    {
        public TContext Context { get; protected set; }

        public NetworkClientID ClientID { get; protected set; }

        public TConnection Connection { get; protected set; }

        public abstract TIID InternalID { get; }

        public NetworkTransportClient(TContext context, NetworkClientID clientID, TConnection session)
        {
            this.Context = context;
            this.ClientID = clientID;
            this.Connection = session;
        }
    }

    #region Delegates
    public delegate void NetworkTransportConnectDelegate(NetworkClientID client);
    public delegate void NetworkTransportMessageDelegate(NetworkClientID client, NetworkMessage message, ArraySegment<byte> raw, DeliveryMode mode);
    public delegate void NetworkTransportDisconnectDelegate(NetworkClientID client);
    #endregion
}