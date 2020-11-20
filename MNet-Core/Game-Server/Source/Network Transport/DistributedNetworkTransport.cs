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
    abstract class DistributedNetworkTransport<TTransport, TContext, TClient, TConnection, TIID> : NetworkTransport<TTransport, TContext, TClient, TConnection, TIID>
        where TTransport : NetworkTransport<TTransport, TContext, TClient, TConnection, TIID>
        where TContext : NetworkTransportContext<TTransport, TContext, TClient, TConnection, TIID>
        where TClient : NetworkTransportClient<TContext, TConnection, TIID>
    {
        public Dictionary<TIID, TClient> Clients { get; protected set; }

        public HashSet<TIID> UnregisteredClients { get; protected set; }

        #region Abstract Utility
        protected abstract TIID GetIID(TConnection connection);

        protected abstract void Send(TConnection connection, params byte[] raw);

        public abstract void Disconnect(TConnection connection, DisconnectCode code);
        #endregion

        protected virtual void MarkUnregisteredConnection(TConnection connection)
        {
            var iid = GetIID(connection);

            UnregisteredClients.Add(iid);
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
                Disconnect(connection, DisconnectCode.InvalidContext);
                return;
            }

            if (Contexts.TryGetValue(contextID, out var context) == false)
            {

                Log.Warning($"Connection {iid} Trying to Register to Non-Registered Context {contextID}");
                Disconnect(connection, DisconnectCode.InvalidContext);
            }

            var client = context.RegisterClient(connection);

            UnregisteredClients.Remove(iid);
            Clients.Add(client.InternalID, client);

            Send(connection, Utility.Registeration.Success);
        }

        protected virtual void ProcessMessage(TConnection connection, byte[] raw, DeliveryMode mode)
        {
            var iid = GetIID(connection);

            if (UnregisteredClients.Contains(iid))
                RegisterConnection(connection, raw);
            else
                RouteMessage(connection, raw, mode);
        }

        protected virtual void RouteMessage(TConnection connection, byte[] raw, DeliveryMode mode)
        {
            var iid = GetIID(connection);

            if (Clients.TryGetValue(iid, out var client) == false)
            {
                Log.Info($"Connection {iid} Not Marked Unregistered but Also Not Registered");
                return;
            }

            var context = client.Context;

            context.RegisterMessages(client, raw, mode);
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

        public DistributedNetworkTransport() : base()
        {
            Clients = new Dictionary<TIID, TClient>();

            UnregisteredClients = new HashSet<TIID>();
        }
    }
}