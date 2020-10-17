using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace MNet
{
    public interface INetworkTransport
    {
        void Start();

        INetworkTransportContext Register(uint id);
        void Unregister(uint id);
    }

    public interface INetworkTransportContext
    {
        void Poll();

        public event NetworkTransportConnectDelegate OnConnect;
        public event NetworkTransportMessageDelegate OnMessage;
        public event NetworkTransportDisconnectDelegate OnDisconnect;

        void Send(NetworkClientID target, byte[] raw);

        void Broadcast(IReadOnlyCollection<NetworkClientID> targets, byte[] raw);
        void Broadcast(byte[] raw);

        void Disconnect(NetworkClientID clientID);
    }

    public delegate void NetworkTransportConnectDelegate(NetworkClientID client);
    public delegate void NetworkTransportMessageDelegate(NetworkClientID client, NetworkMessage message, ArraySegment<byte> raw);
    public delegate void NetworkTransportDisconnectDelegate(NetworkClientID client);

    abstract class NetworkTransport<TTransport, TContext, TClient, TConnection, TIID> : INetworkTransport
        where TTransport : NetworkTransport<TTransport, TContext, TClient, TConnection, TIID>
        where TContext : NetworkTransportContext<TTransport, TContext, TClient, TConnection, TIID>
        where TClient : NetworkTransportClient<TContext, TConnection, TIID>
    {
        public Dictionary<uint, TContext> Contexts { get; protected set; }

        public TContext this[uint code] => Contexts[code];

        public abstract void Start();

        public virtual TContext Register(uint id)
        {
            var context = Create(id);

            Contexts.Add(id, context);

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
        protected virtual void Unregister(TContext context)
        {
            context.Close();

            Contexts.Remove(context.ID);
        }

        public NetworkTransport()
        {
            Contexts = new Dictionary<uint, TContext>();

            Log.Info($"Configuring {GetType().Name}");
        }
    }

    abstract class NetworkTransportContext<TTransport, TContext, TClient, TConnection, TIID> : INetworkTransportContext
        where TContext : NetworkTransportContext<TTransport, TContext, TClient, TConnection, TIID>
        where TClient : NetworkTransportClient<TContext, TConnection, TIID>
    {
        public TTransport Transport { get; protected set; }

        public uint ID { get; protected set; }

        public ConcurrentQueue<Action> InputQueue { get; protected set; }

        public AutoKeyDictionary<NetworkClientID, TClient> Clients { get; protected set; }

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
        void InvokeMessage(TClient client, NetworkMessage message, ArraySegment<byte> raw)
        {
            OnMessage?.Invoke(client.ClientID, message, raw);
        }

        protected virtual void QueueMessage(TClient client, NetworkMessage message, ArraySegment<byte> raw)
        {
            InputQueue.Enqueue(Action);

            void Action() => InvokeMessage(client, message, raw);
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

        public virtual TClient RegisterClient(TConnection connection)
        {
            var id = Clients.Reserve();

            var client = CreateClient(id, connection);

            Clients.Assign(id, client);

            QueueConnect(client);

            return client;
        }
        protected abstract TClient CreateClient(NetworkClientID clientID, TConnection connection);

        public virtual void RegisterMessage(TClient sender, byte[] raw)
        {
            var message = NetworkMessage.Read(raw);

            QueueMessage(sender, message, raw);
        }

        public virtual void UnregisterClient(TClient client)
        {
            QueueDisconnect(client);
        }

        protected virtual void RemoveClient(TClient client)
        {
            Clients.Remove(client.ClientID);

            DestoryClient(client);
        }
        protected virtual void DestoryClient(TClient client) { }

        public virtual void Send(NetworkClientID target, byte[] raw)
        {
            if (Clients.TryGetValue(target, out var client) == false)
            {
                Log.Warning($"No Transport Client Registered With ID: {target}");
                return;
            }

            Send(client, raw);
        }
        public abstract void Send(TClient client, byte[] raw);

        public virtual void Broadcast(byte[] raw) => Broadcast(Clients.Values, raw);
        public virtual void Broadcast(IReadOnlyCollection<NetworkClientID> targets, byte[] raw)
        {
            var collection = new TClient[targets.Count];

            var index = 0;

            foreach (var target in targets)
            {
                if (Clients.TryGetValue(target, out var client) == false)
                    Log.Warning($"No Transport Client Registered With ID: {target}");

                collection[index] = client;

                index += 1;
            }

            Broadcast(collection, raw);
        }
        public virtual void Broadcast(IReadOnlyCollection<TClient> targets, byte[] raw)
        {
            foreach (var target in targets)
            {
                if (target == null) continue;

                Send(target, raw);
            }
        }

        public virtual void Disconnect(NetworkClientID clientID)
        {
            if (Clients.TryGetValue(clientID, out var client) == false)
            {
                Log.Warning($"No Transport Client Registered With ID: {clientID}");
                return;
            }

            Disconnect(client);
        }
        public abstract void Disconnect(TClient client);

        public virtual void Close()
        {

        }

        public NetworkTransportContext(TTransport transport, uint id)
        {
            this.Transport = transport;
            this.ID = id;

            Clients = new AutoKeyDictionary<NetworkClientID, TClient>(NetworkClientID.Increment);

            InputQueue = new ConcurrentQueue<Action>();
        }
    }

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

    abstract class AutoDistributedNetworkTransport<TTransport, TContext, TClient, TConnection, TIID> : NetworkTransport<TTransport, TContext, TClient, TConnection, TIID>
        where TTransport : NetworkTransport<TTransport, TContext, TClient, TConnection, TIID>
        where TContext : NetworkTransportContext<TTransport, TContext, TClient, TConnection, TIID>
        where TClient : NetworkTransportClient<TContext, TConnection, TIID>
    {
        public Dictionary<TIID, TClient> Clients { get; protected set; }

        public HashSet<TIID> UnregisteredClients { get; protected set; }

        public readonly byte[] RegisterClientPayload = new byte[] { 200 };

        readonly Thread thread;
        protected virtual void Run()
        {
            while (true) Tick();
        }

        protected abstract void Tick();

        protected abstract TIID GetIID(TConnection connection);

        protected virtual void MarkUnregisteredConnection(TConnection connection)
        {
            var iid = GetIID(connection);

            UnregisteredClients.Add(iid);
        }

        protected virtual void ProcessMessage(TConnection connection, byte[] raw)
        {
            var iid = GetIID(connection);

            if (UnregisteredClients.Contains(iid))
                RegisterConnection(connection, raw);
            else
                RouteMessage(connection, raw);
        }
        protected virtual void RegisterConnection(TConnection connection, byte[] data)
        {
            var iid = GetIID(connection);

            uint contextID;

            try
            {
                contextID = BitConverter.ToUInt32(data);
            }
            catch (Exception)
            {
                Disconnect(connection);
                return;
            }

            if (Contexts.TryGetValue(contextID, out var context) == false)
            {

                Log.Warning($"Connection {iid} Trying to Register to Non-Registered Context {contextID}");
                Disconnect(connection);
            }

            var client = context.RegisterClient(connection);

            UnregisteredClients.Remove(iid);
            Clients.Add(client.InternalID, client);

            Send(connection, RegisterClientPayload);
        }
        protected virtual void RouteMessage(TConnection connection, byte[] raw)
        {
            var iid = GetIID(connection);

            if (Clients.TryGetValue(iid, out var client) == false)
            {
                Log.Info($"Connection {iid} Not Marked Unregistered but Also Not Registered");
                return;
            }

            var context = client.Context;

            context.RegisterMessage(client, raw);
        }

        protected virtual void RemoveConnection(TConnection connection)
        {
            var iid = GetIID(connection);

            if (UnregisteredClients.Remove(iid)) return;

            if (Clients.TryGetValue(iid, out var client) == false)
            {
                Log.Warning($"Client {iid} Disconnected Without Being Registered Or Marked as Unregistered");
                return;
            }

            var context = client.Context;

            context.UnregisterClient(client);

            Clients.Remove(client.InternalID);
        }

        protected abstract void Send(TConnection connection, byte[] raw);

        protected abstract void Disconnect(TConnection connection);

        public AutoDistributedNetworkTransport() : base()
        {
            Clients = new Dictionary<TIID, TClient>();

            UnregisteredClients = new HashSet<TIID>();

            thread = new Thread(Run);
            thread.Start();
        }
    }
}