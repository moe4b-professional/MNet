using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Server;

using System.Threading;

using System.Collections.Concurrent;
using System.Diagnostics;

namespace MNet
{
    class Room
    {
        #region Info
        public RoomID ID { get; protected set; }

        public AppConfig App { get; protected set; }

        public Version Version { get; protected set; }

        public string Name { get; protected set; }

        public byte Capacity { get; protected set; }
        public byte Occupancy => (byte)Clients.Count;

        public bool IsFull => Occupancy >= Capacity;

        public bool Visibile { get; protected set; }

        public AttributesCollection Attributes { get; protected set; }

        #region Read
        public RoomInfo GetInfo() => new RoomInfo(ID, Name, Capacity, Occupancy, Visibile, Attributes);
        public static RoomInfo GetBasicInfo(Room room) => room.GetInfo();

        NetworkClientInfo[] GetClientsInfo() => Clients.ToArray(NetworkClient.ReadInfo);
        #endregion
        #endregion

        #region Transport
        public bool QueueMessages => App.QueueMessages;

        public INetworkTransportContext TransportContext { get; protected set; }
        #endregion

        #region Time
        Scheduler scheduler;

        public byte TickDelay => App.TickDelay;

        DateTime timestamp;

        NetworkTimeSpan time;
        #endregion

        #region Objects
        Dictionary<NetworkClientID, NetworkClient> Clients;

        AutoKeyDictionary<NetworkEntityID, NetworkEntity> Entities;

        HashSet<NetworkEntity> MasterObjects;
        #endregion

        #region Master
        NetworkClient Master;

        void SetMaster(NetworkClient target)
        {
            Master = target;

            foreach (var entity in MasterObjects) entity.SetOwner(Master);
        }

        void ChangeMaster(NetworkClient target)
        {
            SetMaster(target);

            var command = new ChangeMasterCommand(Master.ID);

            Broadcast(ref command, condition: NetworkClient.IsReady);
        }

        void ChangeMaster()
        {
            if (Clients.Count > 0)
            {
                var target = Clients.Values.First();

                ChangeMaster(target);
            }
            else
            {
                SetMaster(null);
            }
        }
        #endregion

        #region Message Buffer
        NetworkMessageBuffer MessageBuffer;

        public void BufferMessage(NetworkMessage message)
        {
            if (message == null) return;

            MessageBuffer.Add(message);
        }

        public void UnbufferMessage(NetworkMessage message)
        {
            if (message == null) return;

            MessageBuffer.Remove(message);
        }

        public void UnbufferMessages(ICollection<NetworkMessage> collection) => MessageBuffer.RemoveAll(collection.Contains);
        #endregion

        #region Message Dispatcher
        Dictionary<Type, MessageCallbackDelegate> MessageDispatcher;
        public delegate void MessageCallbackDelegate(NetworkClient sender, NetworkMessage message, DeliveryMode mode);

        public delegate void MessageHandler1Delegate<TPayload>(NetworkClient sender, ref TPayload payload, DeliveryMode mode);
        public void RegisterMessageHandler<TPayload>(MessageHandler1Delegate<TPayload> handler)
        {
            var type = typeof(TPayload);

            RegisterMessageHandler(type, Callback);

            void Callback(NetworkClient sender, NetworkMessage message, DeliveryMode mode)
            {
                TPayload payload;

                try
                {
                    payload = message.Read<TPayload>();

                }
                catch (Exception)
                {
                    Log.Error($"Exception Reading {typeof(TPayload)} from Client {sender.ID}");
                    DisconnectClient(sender, DisconnectCode.InvalidData);
                    return;
                }

                handler.Invoke(sender, ref payload, mode);
            }
        }

        public delegate void MessageHandler2Delegate<TPayload>(NetworkClient sender, ref TPayload payload);
        public void RegisterMessageHandler<TPayload>(MessageHandler2Delegate<TPayload> handler)
        {
            var type = typeof(TPayload);

            RegisterMessageHandler(type, Callback);

            void Callback(NetworkClient sender, NetworkMessage message, DeliveryMode mode)
            {
                TPayload payload;

                try
                {
                    payload = message.Read<TPayload>();

                }
                catch (Exception)
                {
                    Log.Error($"Exception Reading {typeof(TPayload)} from Client {sender.ID}");
                    DisconnectClient(sender, DisconnectCode.InvalidData);
                    return;
                }

                handler.Invoke(sender, ref payload);
            }
        }

        public void RegisterMessageHandler(Type type, MessageCallbackDelegate callback)
        {
            if (MessageDispatcher.ContainsKey(type))
                throw new Exception($"Type {type} Already Added to Room's Message Dispatcher");

            MessageDispatcher.Add(type, callback);
        }
        #endregion

        #region Communication
        NetworkMessage Send<T>(ref T payload, NetworkClient target, DeliveryMode mode = DeliveryMode.Reliable)
        {
            var message = NetworkMessage.Write(ref payload);

            var raw = NetworkSerializer.Serialize(message);

            if (QueueMessages)
                QueueMessage(raw, target, mode);
            else
                TransportContext.Send(target.ID, raw, mode);

            return message;
        }

        NetworkMessage Broadcast<T>(ref T payload, DeliveryMode mode = DeliveryMode.Reliable, NetworkClientID? exception1 = null, NetworkClientID? exception2 = null, BroadcastCondition condition = null)
        {
            var message = NetworkMessage.Write(ref payload);

            var raw = NetworkSerializer.Serialize(message);

            if (QueueMessages)
            {
                foreach (var client in Clients.Values)
                {
                    if (exception1 == client.ID) continue;
                    if (exception2 == client.ID) continue;

                    if (condition?.Invoke(client) == false) continue;

                    QueueMessage(raw, client, mode);
                }
            }
            else
            {
                foreach (var client in Clients.Values)
                {
                    if (exception1 == client.ID) continue;
                    if (exception2 == client.ID) continue;

                    if (condition?.Invoke(client) == false) continue;

                    TransportContext.Send(client.ID, raw, mode);
                }
            }

            return message;
        }
        public delegate bool BroadcastCondition(NetworkClient client);

        HashSet<NetworkClient> ClientSendQueue;
        void QueueMessage(byte[] message, NetworkClient target, DeliveryMode mode)
        {
            target.SendQueue.Add(message, mode);

            if (ClientSendQueue.Contains(target) == false) ClientSendQueue.Add(target);
        }

        void ResolveSendQueue()
        {
            foreach (var client in ClientSendQueue)
            {
                var deliveries = client.SendQueue.Deliveries;

                for (int d = 0; d < deliveries.Count; d++)
                {
                    if (deliveries[d].Empty) continue;

                    var buffers = deliveries[d].Read();

                    for (int b = 0; b < buffers.Count; b++)
                        TransportContext.Send(client.ID, buffers[b], deliveries[d].Mode);

                    deliveries[d].Clear();
                }
            }

            ClientSendQueue.Clear();
        }
        #endregion

        List<NetworkMessage> LoadScenesMessages;

        public void Start()
        {
            Log.Info($"Starting Room {ID}");

            timestamp = DateTime.UtcNow;

            RegisterInternalMessageHandlers();

            TransportContext = Realtime.Register(ID.Value);

            TransportContext.OnConnect += ClientConnected;
            TransportContext.OnMessage += MessageRecievedCallback;
            TransportContext.OnDisconnect += ClientDisconnected;

            scheduler.Start();

            OnTick += VoidRoomClearProcedure;
        }

        void RegisterInternalMessageHandlers()
        {
            RegisterMessageHandler<ReadyClientRequest>(ReadyClient);

            RegisterMessageHandler<SpawnEntityRequest>(SpawnEntity);
            RegisterMessageHandler<ChangeEntityOwnerRequest>(ChangeEntityOwner);
            RegisterMessageHandler<DestroyEntityRequest>(DestroyEntity);

            RegisterMessageHandler<RpcRequest>(InvokeRPC);
            RegisterMessageHandler<SyncVarRequest>(InvokeSyncVar);
            RegisterMessageHandler<RprRequest>(InvokeRPR);

            RegisterMessageHandler<RoomTimeRequest>(ProcessTimeRequest);
            RegisterMessageHandler<PingRequest>(ProcessPingRequest);

            RegisterMessageHandler<LoadScenesPayload>(LoadScenes);

            RegisterMessageHandler<ChangeRoomInfoPayload>(ChangeInfo);
        }

        void VoidRoomClearProcedure()
        {
            if (scheduler.ElapsedTime > 10 * 1000)
            {
                OnTick -= VoidRoomClearProcedure;

                if (Occupancy == 0) Stop();
            }
        }

        void ClientConnected(NetworkClientID id)
        {
            Log.Info($"Room {this.ID}: Client {id} Connected");
        }

        public event Action OnTick;
        void Tick()
        {
            time = NetworkTimeSpan.Calculate(timestamp);

            TransportContext.Poll();

            OnTick?.Invoke();

            if (scheduler.Running && QueueMessages) ResolveSendQueue();
        }

        void MessageRecievedCallback(NetworkClientID id, NetworkMessage message, DeliveryMode mode)
        {
            if (Clients.TryGetValue(id, out var client))
            {
                if (MessageDispatcher.TryGetValue(message.Type, out var callback))
                    callback(client, message, mode);
            }
            else
            {
                if (message.Is<RegisterClientRequest>())
                {
                    var request = message.Read<RegisterClientRequest>();

                    RegisterClient(id, request);
                }
            }
        }

        void RegisterClient(NetworkClientID id, RegisterClientRequest request)
        {
            if (IsFull)
            {
                TransportContext.Disconnect(id, DisconnectCode.FullCapacity);
                return;
            }

            if (Clients.ContainsKey(id))
            {
                Log.Warning($"Client {id} Already Registered With Room {this.ID}, Ignoring Register Request");
                return;
            }

            var info = new NetworkClientInfo(id, request.Profile);

            var client = new NetworkClient(info);

            if (Clients.Count == 0) SetMaster(client);

            Clients.Add(id, client);

            Log.Info($"Room {this.ID}: Client {id} Registerd");

            var response = new RegisterClientResponse(id);
            Send(ref response, client);

            var payload = new ClientConnectedPayload(info);
            Broadcast(ref payload, condition: NetworkClient.IsReady);
        }

        void ReadyClient(NetworkClient client, ref ReadyClientRequest request)
        {
            client.SetReady();

            var time = new RoomTimeResponse(this.time, request.Timestamp);

            ///DO NOT PASS the Message Buffer in as an argument for the ReadyClientResponse
            ///You'll get what I can only describe as a very rare single-threaded race condition
            ///In reality this is because the ReadyClientResponse will be serialized later on
            ///And the MessageBuffer.List will get passed by reference
            ///So if a ReadyClientResponse request is created for a certain client before any previous client spawns an entity
            ///The message buffer will still include the new entity spawn
            ///Because by the time the buffer list gets serialized, it would be the latest version in the room
            ///And the client will still recieve the entity spawn command in real-time because they are now marked ready
            ///And yeah ... don't ask me how I know :P
            var buffer = MessageBuffer.List.ToArray();

            var room = GetInfo();
            var clients = GetClientsInfo();

            var response = new ReadyClientResponse(room, clients, Master.ID, buffer, time);

            Send(ref response, client);

            Log.Info($"Room {this.ID}: Client {client.ID} Set Ready");
        }

        void ChangeInfo(NetworkClient sender, ref ChangeRoomInfoPayload payload, DeliveryMode mode)
        {
            if (payload.ModifyVisiblity) Visibile = payload.Visibile;

            if (payload.ModifyAttributes) Attributes.CopyFrom(payload.ModifiedAttributes);

            if (payload.RemoveAttributes) Attributes.RemoveAll(payload.RemovedAttributes);

            Broadcast(ref payload, condition: NetworkClient.IsReady, exception1: sender.ID);
        }

        #region RPC
        void InvokeRPC(NetworkClient sender, ref RpcRequest request, DeliveryMode mode)
        {
            if (Entities.TryGetValue(request.Entity, out var entity) == false)
            {
                Log.Warning($"Client {sender.ID} Trying to Invoke RPC {request.Method} On Unregisterd Entity {request.Entity}");
                if (request.Type == RpcType.Query) ResolveRPR(sender, ref request, RemoteResponseType.InvalidEntity);
                return;
            }

            switch (request.Type)
            {
                case RpcType.Broadcast:
                    InvokeBroadcastRPC(sender, entity, ref request, mode);
                    break;

                case RpcType.Target:
                case RpcType.Query:
                    InvokeDirectRPC(sender, entity, ref request, mode);
                    break;
            }
        }

        void InvokeBroadcastRPC(NetworkClient sender, NetworkEntity entity, ref RpcRequest request, DeliveryMode mode)
        {
            var command = RpcCommand.Write(sender.ID, request);

            var message = Broadcast(ref command, mode: mode, condition: NetworkClient.IsReady, exception1: request.Exception, exception2: sender.ID);

            entity.RpcBuffer.Set(message, ref request, BufferMessage, UnbufferMessages);
        }

        void InvokeDirectRPC(NetworkClient sender, NetworkEntity entity, ref RpcRequest request, DeliveryMode mode)
        {
            if (Clients.TryGetValue(request.Target, out var target) == false)
            {
                Log.Warning($"No NetworkClient With ID {request.Target} Found to Send RPC {request.Method} To");
                if (request.Type == RpcType.Query) ResolveRPR(sender, ref request, RemoteResponseType.InvalidClient);
                return;
            }

            var command = RpcCommand.Write(sender.ID, request);

            Send(ref command, target, mode);
        }
        #endregion

        #region RPR
        void InvokeRPR(NetworkClient sender, ref RprRequest request)
        {
            if (Clients.TryGetValue(request.Target, out var target) == false)
            {
                Log.Warning($"Couldn't Find RPR Target {request.Target}, Most Likely Disconnected Before Getting Answer");
                return;
            }

            var command = RprResponse.Write(sender.ID, request);
            Send(ref command, target);
        }

        void ResolveRPR(NetworkClient requester, ref RpcRequest request, RemoteResponseType response)
        {
            var command = RprCommand.Write(request.ReturnChannel, response);
            Send(ref command, requester);
        }
        #endregion

        #region SyncVar
        void InvokeSyncVar(NetworkClient sender, ref SyncVarRequest request, DeliveryMode mode)
        {
            if (Entities.TryGetValue(request.Entity, out var entity) == false)
            {
                Log.Warning($"Client {sender} Trying to Invoke SyncVar on Non Existing Entity {request.Entity}");
                return;
            }

            var command = SyncVarCommand.Write(sender.ID, request);

            var message = Broadcast(ref command, mode: mode, condition: NetworkClient.IsReady, exception1: sender.ID);

            entity.SyncVarBuffer.Set(message, request, BufferMessage, UnbufferMessage);
        }
        #endregion

        #region Utility Requests
        void ProcessTimeRequest(NetworkClient sender, ref RoomTimeRequest request)
        {
            var response = new RoomTimeResponse(time, request.Timestamp);

            Send(ref response, sender);
        }

        void ProcessPingRequest(NetworkClient sender, ref PingRequest request)
        {
            var response = new PingResponse(request);

            Send(ref response, sender);
        }
        #endregion

        void LoadScenes(NetworkClient sender, ref LoadScenesPayload payload)
        {
            if (sender != Master)
            {
                Log.Warning($"Non Master Client {sender} Trying to Load Scenes in Room, Ignoring");
                return;
            }

            if (payload.Mode == NetworkSceneLoadMode.Single)
            {
                var array = Entities.Values.ToArray();

                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].Persistance.HasFlag(PersistanceFlags.SceneLoad)) continue;

                    DestroyEntity(array[i], false);
                }
            }

            var message = Broadcast(ref payload, condition: NetworkClient.IsReady, exception1: sender.ID);

            if (payload.Mode == NetworkSceneLoadMode.Single)
            {
                UnbufferMessages(LoadScenesMessages);
                LoadScenesMessages.Clear();
            }

            LoadScenesMessages.Add(message);
            BufferMessage(message);
        }

        #region Entity
        void SpawnEntity(NetworkClient sender, ref SpawnEntityRequest request)
        {
            if (request.Type == EntityType.SceneObject && sender != Master)
            {
                Log.Warning($"Non Master Client {sender.ID} Trying to Spawn Scene Object");
                return;
            }

            if (request.Owner != null && sender != Master)
            {
                Log.Warning($"Non Master Client {sender.ID} Trying to Spawn Object for Client {request.Owner}");
                return;
            }

            var id = Entities.Reserve();

            NetworkClient owner;

            if (request.Owner.HasValue)
                Clients.TryGetValue(request.Owner.Value, out owner);
            else
                owner = sender;

            if (owner == null)
            {
                Log.Warning($"No Owner Found For Spawned Entity Request");
                return;
            }

            var entity = new NetworkEntity(owner, id, request.Type, request.Persistance);

            owner?.Entities.Add(entity);

            Entities.Assign(id, entity);

            if (entity.IsMasterObject) MasterObjects.Add(entity);

            Log.Info($"Room {this.ID}: Client {owner.ID} Spawned Entity {entity.ID}");

            if (entity.IsDynamic)
            {
                var response = SpawnEntityResponse.Write(entity.ID, request.Token);
                Send(ref response, sender);
            }

            NetworkClientID? exception = entity.IsDynamic ? sender.ID : null;

            var command = SpawnEntityCommand.Write(owner.ID, entity.ID, request);
            entity.SpawnMessage = Broadcast(ref command, condition: NetworkClient.IsReady, exception1: exception);
            BufferMessage(entity.SpawnMessage);
        }

        void ChangeEntityOwner(NetworkClient sender, ref ChangeEntityOwnerRequest request)
        {
            if (Clients.TryGetValue(request.Client, out var owner) == false)
            {
                Log.Warning($"No Network Client: {request.Client} Found to Take Ownership of Entity {request.Entity}");
                return;
            }

            if (Entities.TryGetValue(request.Entity, out var entity) == false)
            {
                Log.Warning($"No Entity {request.Entity} Found to Change Ownership of, Ignoring request from Client: {sender}");
                return;
            }

            if (entity.IsMasterObject)
            {
                Log.Warning($"Master Objects Cannot be Taken Over by Clients, Ignoring request from Client: {sender}");
                return;
            }

            entity.Owner?.Entities.Remove(entity);
            entity.SetOwner(owner);
            entity.Owner?.Entities.Add(entity);

            UnbufferMessage(entity.OwnershipMessage);

            var command = new ChangeEntityOwnerCommand(owner.ID, request.Entity);
            entity.OwnershipMessage = Broadcast(ref command, condition: NetworkClient.IsReady, exception1: sender.ID);

            BufferMessage(entity.OwnershipMessage);
        }

        void MakeEntityOrphan(NetworkEntity entity)
        {
            if (MessageBuffer.TryGetIndex(entity.SpawnMessage, out var bufferIndex) == false)
            {
                Log.Error($"Trying to Make Entity {entity} an Orphan but It's spawn message was not found in the MessageBuffer, Ignoring");
                return;
            }

            entity.Type = EntityType.Orphan;
            entity.SetOwner(Master);

            MasterObjects.Add(entity);

            var command = entity.SpawnMessage.Read<SpawnEntityCommand>().MakeOrphan();
            var message = NetworkMessage.Write(ref command);
            MessageBuffer.Set(bufferIndex, message);
        }

        void DestroyEntity(NetworkClient sender, ref DestroyEntityRequest request)
        {
            if (Entities.TryGetValue(request.ID, out var entity) == false)
            {
                Log.Warning($"Client {sender} Trying to Destroy Non Registered Entity {request.ID}");
                return;
            }

            if (sender != entity.Owner && sender != Master)
            {
                Log.Warning($"Client {sender} Trying to Destroy Entity {entity} Without Having Authority on that Entity");
                return;
            }

            DestroyEntity(entity, true);
        }
        void DestroyEntity(NetworkEntity entity, bool broadcast)
        {
            UnbufferMessage(entity.SpawnMessage);
            UnbufferMessage(entity.OwnershipMessage);

            entity.RpcBuffer.Clear(UnbufferMessages);
            entity.SyncVarBuffer.Clear(UnbufferMessages);

            entity.Owner?.Entities.Remove(entity);

            Entities.Remove(entity.ID);

            if (entity.IsMasterObject) MasterObjects.Remove(entity);

            var command = new DestroyEntityCommand(entity.ID);
            if (broadcast) Broadcast(ref command, condition: NetworkClient.IsReady);
        }
        #endregion

        void DisconnectClient(NetworkClient client, DisconnectCode code)
        {
            Clients.Remove(client.ID);

            TransportContext.Disconnect(client.ID, code);
        }

        void ClientDisconnected(NetworkClientID id)
        {
            Log.Info($"Room {this.ID}: Client {id} Disconnected");

            if (Clients.TryGetValue(id, out var client)) RemoveClient(client);

            if (Occupancy == 0) Stop();
        }

        void RemoveClient(NetworkClient client)
        {
            var entities = client.Entities;

            for (int i = entities.Count; i-- > 0;)
            {
                if (entities[i].Type == EntityType.SceneObject) continue;

                if (entities[i].Persistance.HasFlag(PersistanceFlags.PlayerDisconnection))
                {
                    MakeEntityOrphan(entities[i]);
                    continue;
                }

                DestroyEntity(client.Entities[i], false);
            }

            Clients.Remove(client.ID);

            ClientSendQueue.Remove(client);

            if (client == Master) ChangeMaster();

            var payload = new ClientDisconnectPayload(client.ID);
            Broadcast(ref payload, condition: NetworkClient.IsReady);
        }

        public delegate void StopDelegate(Room room);
        public event StopDelegate OnStop;
        void Stop()
        {
            Log.Info($"Stopping Room {ID}");

            scheduler.Stop();

            Realtime.Unregister(ID.Value);

            OnStop?.Invoke(this);
        }

        public Room(RoomID id, AppConfig app, Version version, string name, byte capacity, bool visibile, AttributesCollection attributes)
        {
            this.ID = id;

            this.Version = version;
            this.App = app;

            this.Name = name;

            this.Capacity = capacity;

            this.Visibile = visibile;

            this.Attributes = attributes;

            MessageBuffer = new NetworkMessageBuffer();

            MessageDispatcher = new Dictionary<Type, MessageCallbackDelegate>();

            Clients = new Dictionary<NetworkClientID, NetworkClient>();
            Entities = new AutoKeyDictionary<NetworkEntityID, NetworkEntity>(NetworkEntityID.Increment);
            MasterObjects = new HashSet<NetworkEntity>();

            scheduler = new Scheduler(TickDelay, Tick);

            LoadScenesMessages = new List<NetworkMessage>();

            ClientSendQueue = new HashSet<NetworkClient>(capacity);
        }
    }
}