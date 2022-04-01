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

        INetworkTransportContext StartContext(uint id);
        void StopContext(uint id, DisconnectCode code);

        void Stop(DisconnectCode code);
    }

    abstract class NetworkTransport<TTransport, TContext, TClient, TConnection> : INetworkTransport
        where TTransport : NetworkTransport<TTransport, TContext, TClient, TConnection>
        where TContext : NetworkTransportContext<TTransport, TContext, TClient, TConnection>, new()
        where TClient : NetworkTransportClient<TContext, TConnection>, new()
    {
        public TTransport Self { get; }

        public ConcurrentDictionary<uint, TContext> Contexts { get; protected set; }

        public TContext this[uint code] => Contexts[code];

        public abstract int CheckMTU(DeliveryMode mode);

        public abstract void Start();

        INetworkTransportContext INetworkTransport.StartContext(uint id) => StartContext(id);
        public virtual TContext StartContext(uint id)
        {
            var context = new TContext();
            Contexts.TryAdd(id, context);
            context.Configure(Self, id);

            return context;
        }

        public virtual void StopContext(uint id, DisconnectCode code)
        {
            if (Contexts.TryGetValue(id, out var context) == false)
                throw new InvalidOperationException($"No Network Transport Context Registered With {id}");

            StopContext(context, code);
        }
        public virtual void StopContext(TContext context, DisconnectCode code)
        {
            context.Stop(code);
            Contexts.TryRemove(context.ID);
        }

        public virtual void Stop(DisconnectCode code)
        {
            var contexts = Contexts.Values.ToArray();

            foreach (var context in contexts)
                StopContext(context, code);

            Close();
        }
        protected abstract void Close();

        public NetworkTransport()
        {
            Self = this as TTransport;

            Contexts = new ConcurrentDictionary<uint, TContext>();
        }
    }
    #endregion

    #region Context
    public interface INetworkTransportContext
    {
        public event NetworkTransportConnectDelegate OnConnect;
        public event NetworkTransportMessageDelegate OnMessage;
        public event NetworkTransportDisconnectDelegate OnDisconnect;

        bool Send(NetworkClientID target, ArraySegment<byte> segment, DeliveryMode mode, byte channel);

        bool Disconnect(NetworkClientID clientID, DisconnectCode code);
    }

    abstract class NetworkTransportContext<TTransport, TContext, TClient, TConnection> : INetworkTransportContext
        where TTransport : NetworkTransport<TTransport, TContext, TClient, TConnection>
        where TContext : NetworkTransportContext<TTransport, TContext, TClient, TConnection>, new()
        where TClient : NetworkTransportClient<TContext, TConnection>, new()
    {
        public TContext Self { get; }

        public TTransport Transport { get; protected set; }

        public uint ID { get; protected set; }

        #region Client IDs
        public AutoKeyCollection<NetworkClientID> ClientIDs { get; protected set; }

        public NetworkClientID ReserveClientID()
        {
            lock (ClientIDs) return ClientIDs.Reserve();
        }
        public bool FreeClientID(NetworkClientID id)
        {
            lock (ClientIDs) return ClientIDs.Free(id);
        }
        #endregion

        public ConcurrentDictionary<NetworkClientID, TClient> Clients { get; protected set; }

        public virtual void Configure(TTransport transport, uint id)
        {
            this.Transport = transport;
            this.ID = id;
        }

        #region Callbacks
        public event NetworkTransportConnectDelegate OnConnect;
        void InvokeConnect(TClient client)
        {
            OnConnect?.Invoke(client.ID);
        }

        public event NetworkTransportMessageDelegate OnMessage;
        internal void InvokeMessage(TClient client, ArraySegment<byte> segment, DeliveryMode mode, byte channel, Action dispose)
        {
            OnMessage?.Invoke(client.ID, segment, mode, channel, dispose);
        }

        public event NetworkTransportDisconnectDelegate OnDisconnect;
        void InvokeDisconnect(TClient client)
        {
            OnDisconnect?.Invoke(client.ID);
        }
        #endregion

        #region Register & Unregister
        public virtual TClient RegisterClient(TConnection connection)
        {
            var id = ReserveClientID();

            var client = new TClient();
            client.Configure(Self, connection, id);

            Clients.TryAdd(id, client);

            Statistics.Players.Add();

            InvokeConnect(client);

            return client;
        }

        public virtual void UnregisterClient(TClient client)
        {
            InvokeDisconnect(client);

            Clients.TryRemove(client.ID);

            FreeClientID(client.ID);

            Statistics.Players.Remove();
        }
        #endregion

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
        public virtual bool Disconnect(NetworkClientID clientID, DisconnectCode code)
        {
            if (Clients.TryGetValue(clientID, out var client) == false)
                return false;

            Disconnect(client, code);
            return true;
        }

        public abstract void Disconnect(TClient client, DisconnectCode code);
        #endregion

        public virtual void Stop(DisconnectCode code)
        {
            var clients = Clients.Values.ToArray();

            foreach (var client in clients)
                Disconnect(client, code);

            Close();
        }

        protected abstract void Close();

        public NetworkTransportContext()
        {
            Self = this as TContext;

            Clients = new ConcurrentDictionary<NetworkClientID, TClient>();
            ClientIDs = new AutoKeyCollection<NetworkClientID>(NetworkClientID.Min, NetworkClientID.Max, NetworkClientID.Increment, Constants.IdRecycleLifeTime);
        }
    }
    #endregion

    abstract class NetworkTransportClient<TContext, TConnection>
    {
        public TContext Context { get; protected set; }
        public TConnection Connection { get; protected set; }
        public NetworkClientID ID { get; protected set; }

        public virtual void Configure(TContext context, TConnection connection, NetworkClientID id)
        {
            this.Context = context;
            this.Connection = connection;
            this.ID = id;
        }
    }

    #region Delegates
    public delegate void NetworkTransportConnectDelegate(NetworkClientID client);
    public delegate void NetworkTransportMessageDelegate(NetworkClientID client, ArraySegment<byte> segment, DeliveryMode mode, byte channel, Action dispose);
    public delegate void NetworkTransportDisconnectDelegate(NetworkClientID client);
    #endregion
}