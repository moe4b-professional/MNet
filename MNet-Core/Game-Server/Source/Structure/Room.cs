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

        public AttributesCollection Attributes { get; protected set; }

        #region Read
        public RoomBasicInfo GetBasicInfo() => new RoomBasicInfo(ID, Name, Capacity, Occupancy, Attributes);
        public static RoomBasicInfo GetBasicInfo(Room room) => room.GetBasicInfo();

        public RoomInnerInfo GetInnerInfo() => new RoomInnerInfo(TickDelay);
        public static RoomInnerInfo GetInnerInfo(Room room) => room.GetInnerInfo();

        public RoomInfo GetInfo()
        {
            var basic = GetBasicInfo();
            var inner = GetInnerInfo();

            return new RoomInfo(basic, inner);
        }
        public static RoomInfo GetInfo(Room room) => room.GetInfo();

        public NetworkClientInfo[] GetClientsInfo() => Clients.ToArray(NetworkClient.ReadInfo);
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

            Broadcast(command, condition: NetworkClient.IsReady);
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

        public void BufferMessage(NetworkMessage message) => MessageBuffer.Add(message);
        public void BufferMessage(NetworkMessage? message)
        {
            if (message.HasValue) BufferMessage(message);
        }
        
        public void UnbufferMessage(NetworkMessage message) => MessageBuffer.Remove(message);
        public void UnbufferMessage(NetworkMessage? message)
        {
            if (message.HasValue) UnbufferMessage(message.Value);
        }

        public void UnbufferMessages(ICollection<NetworkMessage> collection) => MessageBuffer.RemoveAll(collection.Contains);
        #endregion

        #region Message Dispatcher
        Dictionary<Type, MessageCallbackDelegate> MessageDispatcher;
        public delegate void MessageCallbackDelegate(NetworkClient sender, NetworkMessage message, DeliveryMode mode);

        public delegate void MessageHandler1Delegate<TPayload>(NetworkClient sender, TPayload payload, DeliveryMode mode);
        public void RegisterMessageHandler<TPayload>(MessageHandler1Delegate<TPayload> handler)
        {
            var type = typeof(TPayload);

            RegisterMessageHandler(type, Callback);

            void Callback(NetworkClient sender, NetworkMessage message, DeliveryMode mode)
            {
                var payload = message.Read<TPayload>();

                handler.Invoke(sender, payload, mode);
            }
        }

        public delegate void MessageHandler2Delegate<TPayload>(NetworkClient sender, TPayload payload);
        public void RegisterMessageHandler<TPayload>(MessageHandler2Delegate<TPayload> handler)
        {
            var type = typeof(TPayload);

            RegisterMessageHandler(type, Callback);

            void Callback(NetworkClient sender, NetworkMessage message, DeliveryMode mode)
            {
                var payload = message.Read<TPayload>();

                handler.Invoke(sender, payload);
            }
        }

        public void RegisterMessageHandler(Type type, MessageCallbackDelegate callback)
        {
            if (MessageDispatcher.ContainsKey(type))
                throw new Exception($"Type {type} Already Added to Room's Message Dispatcher");

            MessageDispatcher.Add(type, callback);
        }

        void RegisterInternalMessageHandlers()
        {
            RegisterMessageHandler<ReadyClientRequest>(ReadyClient);

            RegisterMessageHandler<SpawnEntityRequest>(SpawnEntity);
            RegisterMessageHandler<ChangeEntityOwnerRequest>(ChangeEntityOwner);
            RegisterMessageHandler<DestroyEntityRequest>(DestroyEntity);

            RegisterMessageHandler<RpcRequest>(InvokeRPC);
            RegisterMessageHandler<SyncVarRequest>(InvokeSyncVar);

            RegisterMessageHandler<RoomTimeRequest>(ProcessTimeRequest);
            RegisterMessageHandler<PingRequest>(ProcessPingRequest);

            RegisterMessageHandler<LoadScenesRequest>(LoadScenes);
        }
        #endregion

        #region Communication

        #region Send
        NetworkMessage Send<T>(T payload, NetworkClient target, DeliveryMode mode = DeliveryMode.Reliable)
        {
            var message = NetworkMessage.Write(payload);

            if (QueueMessages)
            {
                QueueMessage(message, target, mode);
            }
            else
            {
                var binary = NetworkSerializer.Serialize(message);

                TransportContext.Send(target.ID, binary, mode);
            }

            return message;
        }

        void QueueMessage(NetworkMessage message, NetworkClient target, DeliveryMode mode)
        {
            target.SendQueue.Add(message, mode);

            if (ClientSendQueue.Contains(target) == false) ClientSendQueue.Add(target);
        }
        #endregion

        #region Broadcast
        NetworkMessage Broadcast<T>(T payload, DeliveryMode mode = DeliveryMode.Reliable, NetworkClientID? exception = null, BroadcastCondition condition = null)
        {
            var message = NetworkMessage.Write(payload);

            if (QueueMessages)
            {
                foreach (var client in Clients.Values)
                {
                    if (exception != null && exception == client.ID) continue;

                    if (condition != null && condition(client) == false) continue;

                    QueueMessage(message, client, mode);
                }
            }
            else
            {
                var binary = NetworkSerializer.Serialize(message);

                foreach (var client in Clients.Values)
                {
                    if (exception != null && exception == client.ID) continue;

                    if (condition != null && condition(client) == false) continue;

                    TransportContext.Send(client.ID, binary, mode);
                }
            }

            return message;
        }

        public delegate bool BroadcastCondition(NetworkClient client);
        #endregion

        HashQueue<NetworkClient> ClientSendQueue;

        void ResolveSendQueues()
        {
            while (ClientSendQueue.Count > 0)
            {
                var client = ClientSendQueue.Dequeue();

                var deliveries = client.SendQueue.Deliveries;

                for (int i = 0; i < deliveries.Count; i++)
                {
                    if (deliveries[i].Empty) continue;

                    foreach (var binary in deliveries[i].Serialize())
                        TransportContext.Send(client.ID, binary, deliveries[i].Mode);
                }
            }
        }
        #endregion

        List<NetworkMessage> LoadScenesMessages;

        public void Start()
        {
            Log.Info($"Starting Room {ID}");

            timestamp = DateTime.UtcNow;

            RegisterInternalMessageHandlers();

            TransportContext = RealtimeAPI.Register(ID.Value);

            TransportContext.OnConnect += ClientConnected;
            TransportContext.OnMessage += MessageRecievedCallback;
            TransportContext.OnDisconnect += ClientDisconnected;

            scheduler.Start();
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

            if (scheduler.Running == false) return;

            if (QueueMessages) ResolveSendQueues();
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

        #region Register Client
        void RegisterClient(NetworkClientID id, RegisterClientRequest request) => RegisterClient(id, request.Profile);

        void RegisterClient(NetworkClientID id, NetworkClientProfile profile)
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

            var info = new NetworkClientInfo(id, profile);

            var client = new NetworkClient(info);

            if (Clients.Count == 0) SetMaster(client);

            Clients.Add(id, client);

            Log.Info($"Room {this.ID}: Client {id} Registerd");

            var room = GetInfo();
            var response = new RegisterClientResponse(id, room);
            Send(response, client);

            var payload = new ClientConnectedPayload(info);
            Broadcast(payload, condition: NetworkClient.IsReady);
        }
        #endregion

        void ReadyClient(NetworkClient client, ReadyClientRequest request)
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

            var response = new ReadyClientResponse(GetClientsInfo(), Master.ID, buffer, time);

            Send(response, client);

            Log.Info($"Room {this.ID}: Client {client.ID} Set Ready");
        }

        #region RPC
        void InvokeRPC(NetworkClient sender, RpcRequest request, DeliveryMode mode)
        {
            if (Entities.TryGetValue(request.Entity, out var entity) == false)
            {
                Log.Warning($"Client {sender.ID} Trying to Invoke RPC {request.Method} On Unregisterd Entity {request.Entity}");

                if (request.Type == RpcType.Query) ResolveRPR(sender, request, RprResult.InvalidEntity);

                return;
            }

            switch (request.Type)
            {
                case RpcType.Broadcast:
                    InvokeBroadcastRPC(sender, request, entity, mode);
                    break;

                case RpcType.Target:
                case RpcType.Query:
                case RpcType.Response:
                    InvokeDirectRPC(sender, request, entity, mode);
                    break;
            }
        }

        void InvokeBroadcastRPC(NetworkClient sender, RpcRequest request, NetworkEntity entity, DeliveryMode mode)
        {
            var command = RpcCommand.Write(sender.ID, request, time);

            var message = Broadcast(command, mode: mode, condition: NetworkClient.IsReady, exception: request.Exception);

            entity.RpcBuffer.Set(message, request, BufferMessage, UnbufferMessages);
        }

        void InvokeDirectRPC(NetworkClient sender, RpcRequest request, NetworkEntity entity, DeliveryMode mode)
        {
            var command = RpcCommand.Write(sender.ID, request, time);

            if (Clients.TryGetValue(request.Target, out var target) == false)
            {
                Log.Warning($"No NetworkClient With ID {request.Target} Found to Send RPC {request.Method} To");

                if (request.Type == RpcType.Query) ResolveRPR(sender, request, RprResult.InvalidClient);

                return;
            }

            if (request.Type == RpcType.Query)
            {
                target.RprCache.Register(sender, entity, request.Behaviour, request.Callback);
            }

            if (request.Type == RpcType.Response)
            {
                if (sender.RprCache.Unregister(target, entity, request.Behaviour, request.Method) == false)
                    Log.Warning($"Client {sender} Sending Response to Client {target} On '{entity.ID}->{request.Behaviour}' But no Cached RPC Was Registerd There");
            }

            Send(command, target, mode);
        }
        #endregion

        #region RPR
        void ResolveRPR(NetworkClient requester, RpcRequest request, RprResult result)
        {
            var command = RpcCommand.Write(Master.ID, request.Entity, request.Behaviour, request.Callback, result, time);

            Send(command, requester);
        }

        void ResolveRPR(RprPromise promise, RprResult result)
        {
            var command = RpcCommand.Write(Master.ID, promise.Entity.ID, promise.Behaviour, promise.Callback, result, time);

            Send(command, promise.Requester);
        }
        #endregion

        #region SyncVar
        void InvokeSyncVar(NetworkClient sender, SyncVarRequest request, DeliveryMode mode)
        {
            if (Entities.TryGetValue(request.Entity, out var entity) == false)
            {
                Log.Warning($"Client {sender} Trying to Invoke SyncVar on Non Existing Entity {request.Entity}");
                return;
            }

            var command = SyncVarCommand.Write(sender.ID, request);

            var message = Broadcast(command, mode: mode, condition: NetworkClient.IsReady);

            entity.SyncVarBuffer.Set(message, request, BufferMessage, UnbufferMessage);
        }
        #endregion

        void ProcessTimeRequest(NetworkClient sender, RoomTimeRequest request)
        {
            var response = new RoomTimeResponse(time, request.Timestamp);

            Send(response, sender);
        }

        void ProcessPingRequest(NetworkClient sender, PingRequest request)
        {
            var response = new PingResponse(request);

            Send(response, sender);
        }

        void LoadScenes(NetworkClient sender, LoadScenesRequest request)
        {
            if (sender != Master)
            {
                Log.Warning($"Non Master Client {sender} Trying to Load Scenes in Room, Ignoring");
                return;
            }

            if (request.Mode == NetworkSceneLoadMode.Single)
            {
                var array = Entities.Values.ToArray();

                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].Persistance.HasFlag(PersistanceFlags.SceneLoad)) continue;

                    DestroyEntity(array[i], false);
                }
            }

            var command = LoadScenesCommand.Write(request);

            var message = Broadcast(command, condition: NetworkClient.IsReady);

            if (request.Mode == NetworkSceneLoadMode.Single)
            {
                UnbufferMessages(LoadScenesMessages);
                LoadScenesMessages.Clear();
            }

            LoadScenesMessages.Add(message);
            BufferMessage(message);
        }

        #region Entity
        void SpawnEntity(NetworkClient sender, SpawnEntityRequest request)
        {
            if (request.Type == NetworkEntityType.SceneObject && sender != Master)
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

            var command = SpawnEntityCommand.Write(owner.ID, entity.ID, request);
            entity.SpawnMessage = Broadcast(command, condition: NetworkClient.IsReady);
            BufferMessage(entity.SpawnMessage);
        }

        void ChangeEntityOwner(NetworkClient sender, ChangeEntityOwnerRequest request)
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
            entity.OwnershipMessage = Broadcast(command, condition: NetworkClient.IsReady);
            BufferMessage(entity.OwnershipMessage);
        }

        void MakeEntityOrphan(NetworkEntity entity)
        {
            if (MessageBuffer.TryGetIndex(entity.SpawnMessage, out var bufferIndex) == false)
            {
                Log.Error($"Trying to Make Entity {entity} an Orphan but It's spawn message was not found in the MessageBuffer, Ignoring");
                return;
            }

            entity.Type = NetworkEntityType.Orphan;
            entity.SetOwner(Master);

            MasterObjects.Add(entity);

            var command = entity.SpawnMessage.Read<SpawnEntityCommand>().MakeOrphan();
            var message = NetworkMessage.Write(command);
            MessageBuffer.Set(bufferIndex, message);
        }

        void DestroyEntity(NetworkClient sender, DestroyEntityRequest request)
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
            if (broadcast) Broadcast(command, condition: NetworkClient.IsReady);
        }
        #endregion

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
                if (entities[i].Type == NetworkEntityType.SceneObject) continue;

                if (entities[i].Persistance.HasFlag(PersistanceFlags.PlayerDisconnection))
                {
                    MakeEntityOrphan(entities[i]);
                    continue;
                }

                DestroyEntity(client.Entities[i], false);
            }

            for (int i = 0; i < client.RprCache.Count; i++)
            {
                Log.Info($"Resolving RPR Promise For Disconnecting Client: {client}, Requested by {client.RprCache[i].Requester}");

                ResolveRPR(client.RprCache[i], RprResult.Disconnected);
            }

            Clients.Remove(client.ID);

            if (client == Master) ChangeMaster();

            var payload = new ClientDisconnectPayload(client.ID);
            Broadcast(payload, condition: NetworkClient.IsReady);
        }

        public delegate void StopDelegate(Room room);
        public event StopDelegate OnStop;
        void Stop()
        {
            Log.Info($"Stopping Room {ID}");

            scheduler.Stop();

            RealtimeAPI.Unregister(ID.Value);

            OnStop?.Invoke(this);
        }

        public Room(RoomID id, AppConfig app, Version version, string name, byte capacity, AttributesCollection attributes)
        {
            this.ID = id;

            this.Version = version;
            this.App = app;

            this.Name = name;

            this.Capacity = capacity;

            this.Attributes = attributes;

            MessageBuffer = new NetworkMessageBuffer();

            MessageDispatcher = new Dictionary<Type, MessageCallbackDelegate>();

            Clients = new Dictionary<NetworkClientID, NetworkClient>();
            Entities = new AutoKeyDictionary<NetworkEntityID, NetworkEntity>(NetworkEntityID.Increment);
            MasterObjects = new HashSet<NetworkEntity>();

            scheduler = new Scheduler(TickDelay, Tick);

            LoadScenesMessages = new List<NetworkMessage>();

            ClientSendQueue = new HashQueue<NetworkClient>(capacity);
        }
    }
}