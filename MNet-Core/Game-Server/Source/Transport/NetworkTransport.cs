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
        int CheckMTU(DeliveryMode mode);

        void Start();

        INetworkTransportContext Register(uint id);
        void Unregister(uint id);

        void Stop();
    }

    abstract class NetworkTransport<TTransport, TContext, TClient, TConnection> : INetworkTransport
        where TTransport : NetworkTransport<TTransport, TContext, TClient, TConnection>
        where TContext : NetworkTransportContext<TTransport, TContext, TClient, TConnection>
        where TClient : NetworkTransportClient<TContext, TConnection>
    {
        public ConcurrentDictionary<uint, TContext> Contexts { get; protected set; }

        public TContext this[uint code] => Contexts[code];

        public abstract int CheckMTU(DeliveryMode mode);

        public abstract void Start();

        public virtual TContext Register(uint id)
        {
            var context = CreateContext(id);

            Contexts.TryAdd(id, context);

            return context;
        }
        protected abstract TContext CreateContext(uint id);

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

        public abstract void Stop();

        public NetworkTransport()
        {
            Contexts = new ConcurrentDictionary<uint, TContext>();
        }
    }
    #endregion

    #region Context
    public interface INetworkTransportContext
    {
        INetworkTransport Transport { get; }

        public event NetworkTransportConnectDelegate OnConnect;
        public event NetworkTransportMessageDelegate OnMessage;
        public event NetworkTransportDisconnectDelegate OnDisconnect;

        bool Send(NetworkClientID target, ArraySegment<byte> segment, DeliveryMode mode, byte channel);

        bool Disconnect(NetworkClientID clientID, DisconnectCode code);
        void DisconnectAll(DisconnectCode code);
    }

    abstract class NetworkTransportContext<TTransport, TContext, TClient, TConnection> : INetworkTransportContext
        where TTransport : NetworkTransport<TTransport, TContext, TClient, TConnection>
        where TContext : NetworkTransportContext<TTransport, TContext, TClient, TConnection>
        where TClient : NetworkTransportClient<TContext, TConnection>
    {
        public TTransport Transport { get; protected set; }

        INetworkTransport INetworkTransportContext.Transport => Transport;

        public uint ID { get; protected set; }

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

        #region Callbacks
        public event NetworkTransportConnectDelegate OnConnect;
        internal void InvokeConnect(TClient client)
        {
            OnConnect?.Invoke(client.ID);
        }

        public event NetworkTransportMessageDelegate OnMessage;
        internal void InvokeMessage(TClient client, ArraySegment<byte> segment, DeliveryMode mode, byte channel, Action dispose)
        {
            OnMessage?.Invoke(client.ID, segment, mode, channel, dispose);
        }

        public event NetworkTransportDisconnectDelegate OnDisconnect;
        internal void InvokeDisconnect(TClient client)
        {
            OnDisconnect?.Invoke(client.ID);
        }
        #endregion

        #region Register
        public virtual TClient RegisterClient(TConnection connection)
        {
            var id = ReserveClientID();

            var client = CreateClient(id, connection);

            AddClient(id, connection, client);

            InvokeConnect(client);

            return client;
        }

        void AddClient(NetworkClientID id, TConnection connection, TClient client)
        {
            Clients.TryAdd(id, client);

            Statistics.Players.Add();
        }
        #endregion

        #region Unregister
        public virtual void UnregisterClient(TClient client)
        {
            InvokeDisconnect(client);
            RemoveClient(client);
        }

        void RemoveClient(TClient client)
        {
            Clients.TryRemove(client.ID);

            FreeClientID(client.ID);

            DestroyClient(client);

            Statistics.Players.Remove();
        }
        #endregion

        protected abstract TClient CreateClient(NetworkClientID clientID, TConnection connection);
        protected virtual void DestroyClient(TClient client) { }

        #region Send
        public bool Send(NetworkClientID target, ArraySegment<byte> segment, DeliveryMode mode, byte channel)
        {
            if (Clients.TryGetValue(target, out var client) == false)
                return false;

            Send(client, segment, mode, channel);
            return true;
        }

        public abstract void Send(TClient target, ArraySegment<byte> segment, DeliveryMode mode, byte channel);
        #endregion

        #region Disconnect
        public abstract void Disconnect(TClient client, DisconnectCode code);

        public virtual bool Disconnect(NetworkClientID clientID, DisconnectCode code)
        {
            if (Clients.TryGetValue(clientID, out var client) == false)
                return false;

            Disconnect(client, code);
            return true;
        }
        public virtual void DisconnectAll(DisconnectCode code)
        {
            foreach (var client in Clients.Values)
                Disconnect(client, code);
        }
        #endregion

        public abstract void Close();

        public NetworkTransportContext(TTransport transport, uint id)
        {
            this.Transport = transport;
            this.ID = id;

            ClientIDs = new AutoKeyCollection<NetworkClientID>(NetworkClientID.Min, NetworkClientID.Max, NetworkClientID.Increment, Constants.IdRecycleLifeTime);

            Clients = new ConcurrentDictionary<NetworkClientID, TClient>();
        }
    }
    #endregion

    abstract class NetworkTransportClient<TContext, TConnection>
    {
        public TContext Context { get; protected set; }

        public NetworkClientID ID { get; protected set; }

        public TConnection Connection { get; protected set; }

        public NetworkTransportClient(TContext context, NetworkClientID id, TConnection connection)
        {
            this.Context = context;
            this.ID = id;
            this.Connection = connection;
        }
    }

    #region Delegates
    public delegate void NetworkTransportConnectDelegate(NetworkClientID client);
    public delegate void NetworkTransportMessageDelegate(NetworkClientID client, ArraySegment<byte> segment, DeliveryMode mode, byte channel, Action dispose);
    public delegate void NetworkTransportDisconnectDelegate(NetworkClientID client);
    #endregion
}