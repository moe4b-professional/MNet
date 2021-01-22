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
        public RoomID ID { get; protected set; }

        public AppConfig App { get; protected set; }

        public Version Version { get; protected set; }

        public string Name { get; protected set; }

        public byte Capacity { get; protected set; }
        public byte Occupancy => (byte)Clients.Count;

        public bool IsFull => Occupancy >= Capacity;

        public bool Visible { get; protected set; }

        public AttributesCollection Attributes { get; protected set; }

        public InfoProperty Info = new InfoProperty();
        public class InfoProperty : Property
        {
            public RoomInfo Get() => new RoomInfo(Room.ID, Room.Name, Room.Capacity, Room.Occupancy, Room.Visible, Room.Attributes);

            public override void Configure()
            {
                base.Configure();

                MessageDispatcher.RegisterHandler<ChangeRoomInfoPayload>(Change);
            }

            void Change(NetworkClient sender, ref ChangeRoomInfoPayload payload, DeliveryMode mode)
            {
                if (payload.ModifyVisiblity) Room.Visible = payload.Visibile;

                if (payload.ModifyAttributes) Room.Attributes.CopyFrom(payload.ModifiedAttributes);

                if (payload.RemoveAttributes) Room.Attributes.RemoveAll(payload.RemovedAttributes);

                Room.Broadcast(ref payload, exception1: sender.ID);
            }
        }

        public TimeProperty Time = new TimeProperty();
        public class TimeProperty : Property
        {
            DateTime stamp;

            public NetworkTimeSpan span;

            public TimeResponse CreateResponse(TimeRequest request) => CreateResponse(request.Timestamp);
            public TimeResponse CreateResponse(DateTime stamp) => new TimeResponse(span, stamp);

            public override void Configure()
            {
                base.Configure();

                MessageDispatcher.RegisterHandler<TimeRequest>(Request);
            }

            public override void Start()
            {
                base.Start();

                stamp = DateTime.UtcNow;
            }

            public void Calculate()
            {
                span = NetworkTimeSpan.Calculate(stamp);
            }

            void Request(NetworkClient sender, ref TimeRequest request)
            {
                var response = CreateResponse(request);

                Room.Send(ref response, sender);
            }
        }

        public ClientsProperty Clients = new ClientsProperty();
        public class ClientsProperty : Property
        {
            Dictionary<NetworkClientID, NetworkClient> Dictionary;

            public IReadOnlyCollection<NetworkClient> List => Dictionary.Values;

            public bool TryGet(NetworkClientID id, out NetworkClient client) => Dictionary.TryGetValue(id, out client);

            public bool Contains(NetworkClientID id) => Dictionary.ContainsKey(id);

            public int Count => Dictionary.Count;

            NetworkClientInfo[] GetInfo() => Dictionary.ToArray(NetworkClient.ReadInfo);

            public override void Start()
            {
                base.Start();

                TransportContext.OnConnect += ConnectCallback;
                TransportContext.OnDisconnect += DisconnectCallback;
            }

            #region Connect & Add
            void ConnectCallback(NetworkClientID id)
            {
                Log.Info($"Room {Room.ID}: Client {id} Connected");
            }

            public void Register(NetworkClientID id, ref RegisterClientRequest request)
            {
                if (Room.IsFull)
                {
                    TransportContext.Disconnect(id, DisconnectCode.FullCapacity);
                    return;
                }

                if (Dictionary.ContainsKey(id))
                {
                    Log.Warning($"Client {id} Already Registered With Room {Room.ID}, Ignoring Register Request");
                    return;
                }

                var client = Add(id, request.Profile);

                var time = Time.CreateResponse(request.Time);

                ///DO NOT PASS the Message Buffer in as an argument for the Response
                ///You'll get what I can only describe as a very rare single-threaded race condition
                ///In reality this is because the Response will be serialized later on
                ///And the MessageBuffer.List will get passed by reference
                ///So if a Response request is created for a certain client before any previous client spawns an entity
                ///The message buffer will still include the new entity spawn
                ///Because by the time the buffer list gets serialized, it would be the latest version in the room
                ///And the client will still recieve the entity spawn command in real-time because they are now registered
                ///And yeah ... don't ask me how I found this bug :P
                var buffer = MessageBuffer.ToArray();

                var room = Info.Get();
                var clients = GetInfo();

                Log.Info($"Room {Room.ID}: Client {id} Registerd");

                var response = new RegisterClientResponse(id, room, clients, Master.ID, buffer, time);
                Room.Send(ref response, client);

                var payload = new ClientConnectedPayload(client.ID, client.Profile);
                Room.Broadcast(ref payload);
            }

            NetworkClient Add(NetworkClientID id, NetworkClientProfile profile)
            {
                var client = new NetworkClient(id, profile, TransportContext.Transport);

                if (Clients.Count == 0) Master.Set(client);

                Dictionary.Add(id, client);

                return client;
            }
            #endregion

            #region Disconnect & Remove
            void Disconnect(NetworkClient client, DisconnectCode code)
            {
                Dictionary.Remove(client.ID);

                TransportContext.Disconnect(client.ID, code);
            }

            void DisconnectCallback(NetworkClientID id)
            {
                Log.Info($"Room {Room.ID}: Client {id} Disconnected");

                if (Dictionary.TryGetValue(id, out var client)) Remove(client);

                if (Room.Occupancy == 0) Room.Stop();
            }

            void Remove(NetworkClient client)
            {
                Entities.DestroyFor(client);

                Dictionary.Remove(client.ID);

                SendQueue.Remove(client);

                if (client == Master.Client) Master.Select();

                var payload = new ClientDisconnectPayload(client.ID);
                Room.Broadcast(ref payload);
            }
            #endregion

            public ClientsProperty()
            {
                Dictionary = new Dictionary<NetworkClientID, NetworkClient>();
            }
        }

        public MasterProperty Master = new MasterProperty();
        public class MasterProperty : Property
        {
            public NetworkClient Client { get; protected set; }

            public NetworkClientID ID => Client.ID;

            public void Set(NetworkClient target)
            {
                Client = target;

                Entities.ChangeMaster(Client);
            }

            void Change(NetworkClient target)
            {
                Set(target);

                var command = new ChangeMasterCommand(Client.ID);

                Room.Broadcast(ref command);
            }

            public void Select()
            {
                if (Clients.Count > 0)
                {
                    var target = Clients.List.First();

                    Change(target);
                }
                else
                {
                    Set(null);
                }
            }
        }

        public EntitiesProperty Entities = new EntitiesProperty();
        public class EntitiesProperty : Property
        {
            AutoKeyDictionary<NetworkEntityID, NetworkEntity> Dictionary;

            public IReadOnlyCollection<NetworkEntity> List => Dictionary.Values;

            HashSet<NetworkEntity> MasterObjects;

            public void ChangeMaster(NetworkClient client)
            {
                foreach (var entity in MasterObjects)
                    entity.SetOwner(client);
            }

            public bool TryGet(NetworkEntityID id, out NetworkEntity client) => Dictionary.TryGetValue(id, out client);

            public override void Configure()
            {
                base.Configure();

                MessageDispatcher.RegisterHandler<SpawnEntityRequest>(Spawn);
                MessageDispatcher.RegisterHandler<TransferEntityPayload>(Transfer);
                MessageDispatcher.RegisterHandler<TakeoverEntityRequest>(Takeover);
                MessageDispatcher.RegisterHandler<DestroyEntityPayload>(Destroy);
            }

            public bool CheckAuthority(NetworkEntity entity, NetworkClient client)
            {
                if (client == Master.Client) return true;

                if (client == entity.Owner) return true;

                return false;
            }

            void Spawn(NetworkClient sender, ref SpawnEntityRequest request)
            {
                if (request.Type == EntityType.SceneObject && sender != Master.Client)
                {
                    Log.Warning($"Non Master Client {sender.ID} Trying to Spawn Scene Object");
                    return;
                }

                if (request.Owner != null && sender != Master.Client)
                {
                    Log.Warning($"Non Master Client {sender.ID} Trying to Spawn Object for Client {request.Owner}");
                    return;
                }

                var id = Dictionary.Reserve();

                NetworkClient owner;

                if (request.Owner.HasValue)
                    Clients.TryGet(request.Owner.Value, out owner);
                else
                    owner = sender;

                if (owner == null)
                {
                    Log.Warning($"No Owner Found For Spawned Entity Request");
                    return;
                }

                var entity = new NetworkEntity(owner, id, request.Type, request.Persistance);

                Dictionary.Assign(id, entity);
                owner?.Entities.Add(entity);
                if (entity.IsMasterObject) MasterObjects.Add(entity);

                Log.Info($"Room {Room.ID}: Client {owner.ID} Spawned Entity {entity.ID}");

                if (entity.IsDynamic)
                {
                    var response = SpawnEntityResponse.Write(entity.ID, request.Token);
                    Room.Send(ref response, sender);
                }

                NetworkClientID? exception = entity.IsDynamic ? sender.ID : null;

                var command = SpawnEntityCommand.Write(owner.ID, entity.ID, request);
                entity.SpawnMessage = Room.Broadcast(ref command, exception1: exception);
                MessageBuffer.Add(entity.SpawnMessage);
            }

            #region Ownership
            void Transfer(NetworkClient sender, ref TransferEntityPayload payload)
            {
                if (Dictionary.TryGetValue(payload.Entity, out var entity) == false)
                {
                    Log.Warning($"No Entity {payload.Entity} Found to Transfer Ownership of, Ignoring request from Client: {sender}");
                    return;
                }

                if (Clients.TryGet(payload.Client, out var client) == false)
                {
                    Log.Warning($"No Network Client: {payload.Client} Found to Transfer Entity {entity} to");
                    return;
                }

                if (entity.IsMasterObject)
                {
                    Log.Warning($"Master Objects Cannot be Transfered, Ignoring request from Client: {sender}");
                    return;
                }

                if (CheckAuthority(entity, sender) == false)
                {
                    Log.Warning($"Client {sender} Trying to Transfer Ownership of Entity they have no Authority over");
                    return;
                }

                ChangeOwner(entity, client);

                MessageBuffer.Remove(entity.OwnershipMessage);

                entity.OwnershipMessage = Room.Broadcast(ref payload, exception1: sender.ID);

                MessageBuffer.Remove(entity.OwnershipMessage);
            }

            void Takeover(NetworkClient sender, ref TakeoverEntityRequest request)
            {
                if (Dictionary.TryGetValue(request.Entity, out var entity) == false)
                {
                    Log.Warning($"No Entity {request.Entity} Found to Takeover Ownership of, Ignoring request from Client: {sender}");
                    return;
                }

                if (entity.IsMasterObject)
                {
                    Log.Warning($"Master Objects Cannot be Takenover, Ignoring request from Client: {sender}");
                    return;
                }

                ChangeOwner(entity, sender);

                MessageBuffer.Remove(entity.OwnershipMessage);

                var command = TakeoverEntityCommand.Write(sender.ID, request);
                entity.OwnershipMessage = Room.Broadcast(ref command, exception1: sender.ID);

                MessageBuffer.Add(entity.OwnershipMessage);
            }

            void ChangeOwner(NetworkEntity entity, NetworkClient owner)
            {
                entity.Owner?.Entities.Remove(entity);
                entity.SetOwner(owner);
                entity.Owner?.Entities.Add(entity);
            }
            #endregion

            public void MakeOrphan(NetworkEntity entity)
            {
                if (MessageBuffer.TryGetIndex(entity.SpawnMessage, out var bufferIndex) == false)
                {
                    Log.Error($"Trying to Make Entity {entity} an Orphan but It's spawn message was not found in the MessageBuffer, Ignoring");
                    return;
                }

                entity.Type = EntityType.Orphan;
                entity.SetOwner(Master.Client);

                MasterObjects.Add(entity);

                var command = entity.SpawnMessage.Read<SpawnEntityCommand>().MakeOrphan();
                var message = NetworkMessage.Write(ref command);
                MessageBuffer.Set(bufferIndex, message);
            }

            #region Destroy
            void Destroy(NetworkClient sender, ref DestroyEntityPayload payload)
            {
                if (Dictionary.TryGetValue(payload.ID, out var entity) == false)
                {
                    Log.Warning($"Client {sender} Trying to Destroy Non Registered Entity {payload.ID}");
                    return;
                }

                if (CheckAuthority(entity, sender) == false)
                {
                    Log.Warning($"Client {sender} Trying to Destroy Entity {entity} Without Having Authority over that Entity");
                    return;
                }

                Destroy(entity);

                Room.Broadcast(ref payload, exception1: sender.ID);
            }

            public void Destroy(NetworkEntity entity)
            {
                MessageBuffer.Remove(entity.SpawnMessage);
                MessageBuffer.Remove(entity.OwnershipMessage);

                entity.RpcBuffer.Clear(MessageBuffer.RemoveAll);
                entity.SyncVarBuffer.Clear(MessageBuffer.RemoveAll);

                entity.Owner?.Entities.Remove(entity);

                Dictionary.Remove(entity.ID);

                if (entity.IsMasterObject) MasterObjects.Remove(entity);
            }

            public void DestroyFor(NetworkClient client)
            {
                var targets = client.Entities;

                for (int i = targets.Count; i-- > 0;)
                {
                    if (targets[i].Type == EntityType.SceneObject) continue;

                    if (targets[i].Persistance.HasFlag(PersistanceFlags.PlayerDisconnection))
                    {
                        MakeOrphan(targets[i]);
                        continue;
                    }

                    Destroy(client.Entities[i]);
                }
            }

            public void DestroyAllNonPersistant()
            {
                var targets = List.ToArray();

                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i].Persistance.HasFlag(PersistanceFlags.SceneLoad)) continue;

                    Destroy(targets[i]);
                }
            }
            #endregion

            public EntitiesProperty()
            {
                Dictionary = new AutoKeyDictionary<NetworkEntityID, NetworkEntity>(NetworkEntityID.Increment);
                MasterObjects = new HashSet<NetworkEntity>();
            }
        }

        public RemoteCallsProperty RemoteCalls = new RemoteCallsProperty();
        public class RemoteCallsProperty : Property
        {
            public override void Configure()
            {
                base.Configure();

                MessageDispatcher.RegisterHandler<RpcRequest>(InvokeRPC);
                MessageDispatcher.RegisterHandler<SyncVarRequest>(InvokeSyncVar);
                MessageDispatcher.RegisterHandler<RprRequest>(InvokeRPR);
            }

            #region RPC
            void InvokeRPC(NetworkClient sender, ref RpcRequest request, DeliveryMode mode)
            {
                if (Entities.TryGet(request.Entity, out var entity) == false)
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

                var message = Room.Broadcast(ref command, mode: mode, exception1: request.Exception, exception2: sender.ID);

                entity.RpcBuffer.Set(message, ref request, MessageBuffer.Add, MessageBuffer.RemoveAll);
            }

            void InvokeDirectRPC(NetworkClient sender, NetworkEntity entity, ref RpcRequest request, DeliveryMode mode)
            {
                if (Clients.TryGet(request.Target, out var target) == false)
                {
                    Log.Warning($"No NetworkClient With ID {request.Target} Found to Send RPC {request.Method} To");
                    if (request.Type == RpcType.Query) ResolveRPR(sender, ref request, RemoteResponseType.InvalidClient);
                    return;
                }

                var command = RpcCommand.Write(sender.ID, request);

                Room.Send(ref command, target, mode);
            }
            #endregion

            #region RPR
            void InvokeRPR(NetworkClient sender, ref RprRequest request)
            {
                if (Clients.TryGet(request.Target, out var target) == false)
                {
                    Log.Warning($"Couldn't Find RPR Target {request.Target}, Most Likely Disconnected Before Getting Answer");
                    return;
                }

                var command = RprResponse.Write(sender.ID, request);
                Room.Send(ref command, target);
            }

            void ResolveRPR(NetworkClient requester, ref RpcRequest request, RemoteResponseType response)
            {
                var command = RprCommand.Write(request.ReturnChannel, response);
                Room.Send(ref command, requester);
            }
            #endregion

            #region SyncVar
            void InvokeSyncVar(NetworkClient sender, ref SyncVarRequest request, DeliveryMode mode)
            {
                if (Entities.TryGet(request.Entity, out var entity) == false)
                {
                    Log.Warning($"Client {sender} Trying to Invoke SyncVar on Non Existing Entity {request.Entity}");
                    return;
                }

                var command = SyncVarCommand.Write(sender.ID, request);

                var message = Room.Broadcast(ref command, mode: mode, exception1: sender.ID);

                entity.SyncVarBuffer.Set(message, request, MessageBuffer.Add, MessageBuffer.Remove);
            }
            #endregion
        }

        public MessageBufferProperty MessageBuffer = new MessageBufferProperty();
        public class MessageBufferProperty : Property
        {
            public List<NetworkMessage> List { get; protected set; }

            public NetworkMessage[] ToArray() => List.ToArray();

            public bool TryGetIndex(NetworkMessage message, out int index)
            {
                for (index = 0; index < List.Count; index++)
                {
                    if (Equals(List[index], message))
                        return true;
                }

                return false;
            }

            public void Set(int index, NetworkMessage message)
            {
                List[index] = message;
            }

            public void Add(NetworkMessage message)
            {
                if (message == null) return;

                List.Add(message);
            }

            public void Remove(NetworkMessage message)
            {
                if (message == null) return;

                List.Remove(message);
            }

            public void RemoveAll(ICollection<NetworkMessage> collection) => RemoveAll(collection.Contains);
            public int RemoveAll(Predicate<NetworkMessage> predicate)
            {
                return List.RemoveAll(predicate);
            }

            public MessageBufferProperty()
            {
                List = new List<NetworkMessage>();
            }
        }

        public MessageDispatcherProperty MessageDispatcher = new MessageDispatcherProperty();
        public class MessageDispatcherProperty : Property
        {
            Dictionary<Type, MessageCallbackDelegate> Dictionary;
            public delegate void MessageCallbackDelegate(NetworkClient sender, NetworkMessage message, DeliveryMode mode);

            public void Invoke(NetworkClient sender, NetworkMessage message, DeliveryMode mode)
            {
                if (Dictionary.TryGetValue(message.Type, out var callback))
                    callback(sender, message, mode);
                else
                    Log.Warning($"No Message Handler Registered for Payload {message.Type}");
            }

            #region Register
            public delegate void MessageHandler1Delegate<TPayload>(NetworkClient sender, ref TPayload payload, DeliveryMode mode);
            public void RegisterHandler<TPayload>(MessageHandler1Delegate<TPayload> handler)
            {
                var type = typeof(TPayload);

                RegisterHandler(type, Callback);

                void Callback(NetworkClient sender, NetworkMessage message, DeliveryMode mode)
                {
                    var payload = message.Read<TPayload>();

                    handler.Invoke(sender, ref payload, mode);
                }
            }

            public delegate void MessageHandler2Delegate<TPayload>(NetworkClient sender, ref TPayload payload);
            public void RegisterHandler<TPayload>(MessageHandler2Delegate<TPayload> handler)
            {
                var type = typeof(TPayload);

                RegisterHandler(type, Callback);

                void Callback(NetworkClient sender, NetworkMessage message, DeliveryMode mode)
                {
                    var payload = message.Read<TPayload>();

                    handler.Invoke(sender, ref payload);
                }
            }

            public void RegisterHandler(Type type, MessageCallbackDelegate callback)
            {
                if (Dictionary.ContainsKey(type))
                    throw new Exception($"Type {type} Already Added to Room's Message Dispatcher");

                Dictionary.Add(type, callback);
            }
            #endregion

            public MessageDispatcherProperty()
            {
                Dictionary = new Dictionary<Type, MessageCallbackDelegate>();
            }
        }

        public SendQueueProperty SendQueue = new SendQueueProperty();
        public class SendQueueProperty : Property
        {
            HashSet<NetworkClient> hash;

            public void Add(byte[] message, NetworkClient target, DeliveryMode mode)
            {
                target.SendQueue.Add(message, mode);

                hash.Add(target);
            }

            public void Resolve()
            {
                foreach (var client in hash)
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

                hash.Clear();
            }

            public void Remove(NetworkClient client) => hash.Remove(client);

            public SendQueueProperty()
            {
                hash = new HashSet<NetworkClient>();
            }
        }

        public ScenesProperty Scenes = new ScenesProperty();
        public class ScenesProperty : Property
        {
            List<NetworkMessage> LoadMessages;

            public override void Configure()
            {
                base.Configure();

                MessageDispatcher.RegisterHandler<LoadScenesPayload>(Load);
            }

            void Load(NetworkClient sender, ref LoadScenesPayload payload)
            {
                if (sender != Master.Client)
                {
                    Log.Warning($"Non Master Client {sender} Trying to Load Scenes in Room, Ignoring");
                    return;
                }

                if (payload.Mode == NetworkSceneLoadMode.Single) Entities.DestroyAllNonPersistant();

                var message = Room.Broadcast(ref payload, exception1: sender.ID);

                if (payload.Mode == NetworkSceneLoadMode.Single)
                {
                    MessageBuffer.RemoveAll(LoadMessages);
                    LoadMessages.Clear();
                }

                LoadMessages.Add(message);
                MessageBuffer.Add(message);
            }

            public ScenesProperty()
            {
                LoadMessages = new List<NetworkMessage>();
            }
        }

        void ForAllProperties(Action<Property> action)
        {
            action(Info);
            action(Time);
            action(Clients);
            action(Master);
            action(Entities);
            action(RemoteCalls);
            action(MessageBuffer);
            action(MessageDispatcher);
            action(SendQueue);
            action(Scenes);
        }

        public class Property
        {
            public Room Room;

            #region Properties
            public InfoProperty Info => Room.Info;

            public ClientsProperty Clients => Room.Clients;

            public MasterProperty Master => Room.Master;

            public EntitiesProperty Entities => Room.Entities;

            public MessageDispatcherProperty MessageDispatcher => Room.MessageDispatcher;

            public MessageBufferProperty MessageBuffer => Room.MessageBuffer;

            public SendQueueProperty SendQueue => Room.SendQueue;

            public TimeProperty Time => Room.Time;
            #endregion

            public INetworkTransportContext TransportContext => Room.TransportContext;

            public virtual void Set(Room reference) => Room = reference;

            public virtual void Configure() { }

            public virtual void Start() { }
        }

        Scheduler Scheduler;

        public INetworkTransportContext TransportContext;

        #region Communication
        NetworkMessage Send<T>(ref T payload, NetworkClient target, DeliveryMode mode = DeliveryMode.Reliable)
        {
            var message = NetworkMessage.Write(ref payload);

            var raw = NetworkSerializer.Serialize(message);

            if (App.QueueMessages)
                SendQueue.Add(raw, target, mode);
            else
                TransportContext.Send(target.ID, raw, mode);

            return message;
        }

        NetworkMessage Broadcast<T>(ref T payload, DeliveryMode mode = DeliveryMode.Reliable, NetworkClientID? exception1 = null, NetworkClientID? exception2 = null)
        {
            var message = NetworkMessage.Write(ref payload);

            var raw = NetworkSerializer.Serialize(message);

            if (App.QueueMessages)
            {
                foreach (var client in Clients.List)
                {
                    if (exception1 == client.ID) continue;
                    if (exception2 == client.ID) continue;

                    SendQueue.Add(raw, client, mode);
                }
            }
            else
            {
                foreach (var client in Clients.List)
                {
                    if (exception1 == client.ID) continue;
                    if (exception2 == client.ID) continue;

                    TransportContext.Send(client.ID, raw, mode);
                }
            }

            return message;
        }
        #endregion

        public void Start()
        {
            Log.Info($"Starting Room {ID}");

            MessageDispatcher.RegisterHandler<PingRequest>(Ping);

            TransportContext = Realtime.RegisterContext(App.Transport, ID.Value);
            TransportContext.OnMessage += MessageRecievedCallback;

            ForAllProperties(x => x.Start());

            Scheduler.Start();

            OnTick += VoidClearProcedure;
        }

        void VoidClearProcedure()
        {
            if (Scheduler.ElapsedTime > 10 * 1000)
            {
                OnTick -= VoidClearProcedure;

                if (Occupancy == 0) Stop();
            }
        }

        public event Action OnTick;
        void Tick()
        {
            Time.Calculate();

            TransportContext.Poll();

            OnTick?.Invoke();

            if (Scheduler.Running && App.QueueMessages) SendQueue.Resolve();
        }

        void MessageRecievedCallback(NetworkClientID id, NetworkMessage message, DeliveryMode mode)
        {
            if (Clients.TryGet(id, out var sender))
            {
                MessageDispatcher.Invoke(sender, message, mode);
            }
            else
            {
                if (message.Is<RegisterClientRequest>())
                {
                    var request = message.Read<RegisterClientRequest>();

                    Clients.Register(id, ref request);
                }
            }
        }

        void Ping(NetworkClient sender, ref PingRequest request)
        {
            var response = new PingResponse(request);

            Send(ref response, sender);
        }

        public delegate void StopDelegate(Room room);
        public event StopDelegate OnStop;
        void Stop()
        {
            Log.Info($"Stopping Room {ID}");

            Scheduler.Stop();

            Realtime.UnregisterContext(App.Transport, ID.Value);

            OnStop?.Invoke(this);
        }

        public Room(RoomID id, AppConfig app, Version version, string name, byte capacity, bool visible, AttributesCollection attributes)
        {
            this.ID = id;

            this.Version = version;
            this.App = app;

            this.Name = name;
            this.Capacity = capacity;
            this.Visible = visible;
            this.Attributes = attributes;

            Scheduler = new Scheduler(App.TickDelay, Tick);

            ForAllProperties(x => x.Set(this));
            ForAllProperties(x => x.Configure());
        }
    }
}