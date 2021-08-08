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

        public string Password { get; protected set; }

        public bool Locked => string.IsNullOrEmpty(Password) == false;

        public AttributesCollection Attributes { get; protected set; }

        public MigrationPolicy MigrationPolicy { get; protected set; }

        public InfoProperty Info = new InfoProperty();
        public class InfoProperty : Property
        {
            public RoomInfo Get() => new RoomInfo(Room.ID, Room.Name, Room.Capacity, Room.Occupancy, Room.Visible, Room.Locked, Room.Attributes);

            public override void Start()
            {
                base.Start();

                MessageDispatcher.RegisterHandler<ChangeRoomInfoPayload>(Change);
            }

            void Change(NetworkClient sender, ref ChangeRoomInfoPayload payload)
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

            public NetworkTimeSpan Span;

            public TimeResponse CreateResponse(TimeRequest request) => CreateResponse(request.Timestamp);
            public TimeResponse CreateResponse(DateTime stamp) => new TimeResponse(Span, stamp);

            public override void Start()
            {
                base.Start();

                stamp = DateTime.UtcNow;

                MessageDispatcher.RegisterHandler<TimeRequest>(ProcessRequest);
            }

            public void Calculate()
            {
                Span = NetworkTimeSpan.Calculate(stamp);
            }

            void ProcessRequest(NetworkClient sender, ref TimeRequest request)
            {
                var response = CreateResponse(request);

                Room.Send(ref response, sender);
            }
        }

        public ClientsProperty Clients = new ClientsProperty();
        public class ClientsProperty : Property
        {
            Dictionary<NetworkClientID, NetworkClient> Dictionary;

            public List<NetworkClient> List;

            public bool TryGet(NetworkClientID id, out NetworkClient client) => Dictionary.TryGetValue(id, out client);

            public bool Contains(NetworkClientID id) => Dictionary.ContainsKey(id);

            public NetworkClient this[int index] => List[index];

            public int Count => List.Count;

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
                if(Room.Locked)
                {
                    if (Room.Password != request.Password)
                    {
                        Disconnect(id, DisconnectCode.WrongPassword);
                        return;
                    }
                }

                if (Room.IsFull)
                {
                    Disconnect(id, DisconnectCode.NoCapacity);
                    return;
                }

                if (Dictionary.ContainsKey(id))
                {
                    Log.Warning($"Client {id} Already Registered With Room {Room.ID}, Ignoring Register Request");
                    return;
                }

                Log.Info($"Room {Room.ID}: Client {id} Registerd");

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

                var response = new RegisterClientResponse(id, room, clients, Master.ID, buffer, time);
                Room.Send(ref response, client);

                var payload = new ClientConnectedPayload(client.ID, client.Profile);
                Room.Broadcast(ref payload, exception1: client.ID);
            }

            NetworkClient Add(NetworkClientID id, NetworkClientProfile profile)
            {
                var client = new NetworkClient(id, profile, TransportContext.Transport);

                if (Clients.Count == 0) Master.Set(client);

                Dictionary.Add(id, client);
                List.Add(client);

                return client;
            }
            #endregion

            #region Disconnect & Remove
            void Disconnect(NetworkClient client, DisconnectCode code) => Disconnect(client.ID, code);
            void Disconnect(NetworkClientID id, DisconnectCode code) => TransportContext.Disconnect(id, code);

            void DisconnectCallback(NetworkClientID id)
            {
                Log.Info($"Room {Room.ID}: Client {id} Disconnected");

                if (Dictionary.TryGetValue(id, out var client)) Remove(client);

                if (Room.Occupancy == 0 && Room.IsRunning) Room.Stop();
            }

            void Remove(NetworkClient client)
            {
                Entities.DestroyForClient(client);

                Dictionary.Remove(client.ID);
                List.Remove(client);

                SendQueue.Remove(client);

                if (client == Master.Client) ProcessMigration();

                var payload = new ClientDisconnectPayload(client.ID);
                Room.Broadcast(ref payload);
            }
            #endregion

            void ProcessMigration()
            {
                switch (Room.MigrationPolicy)
                {
                    case MigrationPolicy.Continue:
                        Master.ChooseNew();
                        break;

                    case MigrationPolicy.Stop:
                        TransportContext.DisconnectAll(DisconnectCode.HostDisconnected);
                        Room.Stop();
                        break;
                }
            }

            public ClientsProperty()
            {
                Dictionary = new Dictionary<NetworkClientID, NetworkClient>();
                List = new List<NetworkClient>();
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

            public void ChooseNew()
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

        public GroupsProperty Groups = new GroupsProperty();
        public class GroupsProperty : Property
        {
            public override void Start()
            {
                base.Start();

                MessageDispatcher.RegisterHandler<JoinNetworkGroupsPayload>(Join);
                MessageDispatcher.RegisterHandler<LeaveNetworkGroupsPayload>(Leave);
            }

            void Join(NetworkClient sender, ref JoinNetworkGroupsPayload payload)
            {
                for (int i = 0; i < payload.Length; i++)
                    sender.Groups.Add(payload[i]);
            }

            void Leave(NetworkClient sender, ref LeaveNetworkGroupsPayload payload)
            {
                sender.Groups.RemoveWhere(payload.Selection.Contains);
            }
        }

        public ScenesProperty Scenes = new ScenesProperty();
        public class ScenesProperty : Property
        {
            public Dictionary<byte, Scene> Dictionary;
            public List<Scene> List;

            public int Count => Dictionary.Count;

            public bool TryGet(byte index, out Scene scene) => Dictionary.TryGetValue(index, out scene);

            public Scene Active;

            public override void Start()
            {
                base.Start();

                MessageDispatcher.RegisterHandler<LoadScenePayload>(Load);
                MessageDispatcher.RegisterHandler<UnloadScenePayload>(Unload);
            }

            void Load(NetworkClient sender, ref LoadScenePayload payload)
            {
                if (sender != Master.Client)
                {
                    Log.Warning($"Non Master Client {sender} Trying to Load Scenes in Room, Ignoring");
                    return;
                }

                if (Dictionary.ContainsKey(payload.Index) && payload.Mode == NetworkSceneLoadMode.Additive)
                {
                    Log.Warning($"Scene {payload.Index} Already Loaded, Cannot Load the Same Scene Additively Multiple Times");
                    return;
                }

                if (payload.Mode == NetworkSceneLoadMode.Single) RemoveAll();

                var message = Room.Broadcast(ref payload, exception1: sender.ID);

                Add(payload.Index, payload.Mode, message);
            }

            void Unload(NetworkClient sender, ref UnloadScenePayload payload)
            {
                if (sender != Master.Client)
                {
                    Log.Warning($"Non Master Client {sender} Trying to Unload Scenes in Room, Ignoring");
                    return;
                }

                if (Dictionary.TryGetValue(payload.Index, out var scene) == false)
                {
                    Log.Warning($"Cannot Unload Scene {payload.Index} Because It's not Loaded");
                    return;
                }

                if (Count == 1)
                {
                    Log.Warning($"Cannot Unload Scene {payload.Index} as It's the only Loaded Scene");
                    return;
                }

                Remove(scene);

                Room.Broadcast(ref payload, exception1: sender.ID);
            }

            Scene Add(byte index, NetworkSceneLoadMode loadMode, NetworkMessage loadMessage)
            {
                var scene = new Scene(index, loadMode, loadMessage);

                Dictionary.Add(index, scene);
                List.Add(scene);

                MessageBuffer.Add(loadMessage);

                if (Active == null) SelectActive();

                return scene;
            }

            void Remove(Scene scene)
            {
                Entities.DestroyInScene(scene);

                Dictionary.Remove(scene.Index);
                List.Remove(scene);

                MessageBuffer.Remove(scene.LoadMessage);

                if (scene == Active)
                {
                    SelectActive();

                    if (Active != null) ModifyActive();
                }
            }

            void RemoveAll()
            {
                for (int i = List.Count; i-- > 0;)
                    Remove(List[i]);
            }

            void SelectActive()
            {
                Active = List.FirstOrDefault();
            }

            void ModifyActive()
            {
                var payload = Active.LoadMessage.Read<LoadScenePayload>().SetMode(NetworkSceneLoadMode.Single);

                Active.LoadMessage.Set(payload);
            }

            public ScenesProperty()
            {
                Dictionary = new Dictionary<byte, Scene>();
                List = new List<Scene>();
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

            public override void Start()
            {
                base.Start();

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

                if (FindSceneFrom(ref request, out var scene) == false) return;

                var id = Dictionary.Reserve();

                var entity = new NetworkEntity(sender, id, request.Type, request.Persistance, scene);

                sender.Entities.Add(entity);
                Dictionary.Assign(id, entity);
                scene?.Entities.Add(entity);
                if (entity.IsMasterObject) MasterObjects.Add(entity);

                Log.Info($"Room {Room.ID}: Client {sender.ID} Spawned Entity {entity.ID}");

                if (entity.IsDynamic)
                {
                    var response = SpawnEntityResponse.Write(entity.ID, request.Token);
                    Room.Send(ref response, sender);
                }

                NetworkClientID? exception = entity.IsDynamic ? sender.ID : null;

                var command = SpawnEntityCommand.Write(sender.ID, entity.ID, request);
                entity.SpawnMessage = Room.Broadcast(ref command, exception1: exception);
                MessageBuffer.Add(entity.SpawnMessage);
            }

            bool FindSceneFrom(ref SpawnEntityRequest request, out Scene scene)
            {
                switch (request.Type)
                {
                    case EntityType.SceneObject:
                        {
                            if (Scenes.TryGet(request.Scene, out scene) == false)
                            {
                                Log.Warning($"Scene {request.Scene} Not Loaded, Cannot Spawn Scene Object");
                                return false;
                            }

                            return true;
                        }

                    case EntityType.Dynamic:
                        {
                            if (request.Persistance.HasFlag(PersistanceFlags.SceneLoad))
                            {
                                scene = null;
                                return true;
                            }

                            if (Scenes.Active == null)
                            {
                                Log.Warning("Cannot Spawn Entity, No Active Scene Loaded");
                                scene = null;
                                return false;
                            }

                            scene = Scenes.Active;
                            return true;
                        }

                    default:
                        throw new NotImplementedException($"No Condition Set For {request.Type}");
                }
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
                entity.Type = EntityType.Orphan;
                entity.SetOwner(Master.Client);

                MasterObjects.Add(entity);

                var command = entity.SpawnMessage.Read<SpawnEntityCommand>().MakeOrphan();
                entity.SpawnMessage.Set(command);
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

                Dictionary.Remove(entity.ID);
                entity.Owner?.Entities.Remove(entity);
                entity.Scene?.Entities.Remove(entity);
                if (entity.IsMasterObject) MasterObjects.Remove(entity);
            }

            public void DestroyForClient(NetworkClient client)
            {
                var entities = client.Entities.ToArray();

                for (int i = 0; i < entities.Length; i++)
                {
                    if (entities[i].Type == EntityType.SceneObject) continue;

                    if (entities[i].Persistance.HasFlag(PersistanceFlags.PlayerDisconnection))
                    {
                        MakeOrphan(entities[i]);
                        continue;
                    }

                    Destroy(entities[i]);
                }
            }

            public void DestroyInScene(Scene scene)
            {
                var entities = scene.Entities.ToArray();

                for (int i = 0; i < entities.Length; i++)
                    Destroy(entities[i]);
            }
            #endregion

            public EntitiesProperty()
            {
                Dictionary = new AutoKeyDictionary<NetworkEntityID, NetworkEntity>(NetworkEntityID.Increment);
                MasterObjects = new HashSet<NetworkEntity>();
            }
        }

        public SystemProperty System = new SystemProperty();
        public class SystemProperty : Property
        {
            public void SendMessage(NetworkClient target, string text)
            {
                var payload = new SystemMessagePayload(text);

                SendMessage(target, payload);
            }
            public void SendMessage(NetworkClient target, SystemMessagePayload payload) => Room.Send(ref payload, target);

            public void BroadcastMessage(string text, NetworkGroupID group = default)
            {
                var payload = new SystemMessagePayload(text);

                BroadcastMessage(payload, group);
            }
            public void BroadcastMessage(SystemMessagePayload payload, NetworkGroupID group = default) => Room.Broadcast(ref payload, group: group);
        }

        public RemoteCallsProperty RemoteCalls = new RemoteCallsProperty();
        public class RemoteCallsProperty : Property
        {
            public override void Start()
            {
                base.Start();

                MessageDispatcher.RegisterHandler<BroadcastRpcRequest>(InvokeBroadcastRPC);
                MessageDispatcher.RegisterHandler<TargetRpcRequest>(InvokeTargetRPC);
                MessageDispatcher.RegisterHandler<QueryRpcRequest>(InvokeQueryRPC);
                MessageDispatcher.RegisterHandler<BufferRpcRequest>(InvokeBufferRPC);

                MessageDispatcher.RegisterHandler<RprRequest>(InvokeRPR);

                MessageDispatcher.RegisterHandler<BroadcastSyncVarRequest>(InvokeBroadcastSyncVar);
                MessageDispatcher.RegisterHandler<BufferSyncVarRequest>(InvokeBufferSyncVar);
            }

            #region RPC
            void InvokeBroadcastRPC(NetworkClient sender, ref BroadcastRpcRequest request, DeliveryMode mode, byte channel)
            {
                if (Entities.TryGet(request.Entity, out var entity) == false)
                {
                    Log.Warning($"Client {sender.ID} Trying to Invoke RPC {request.Method} On Unregisterd Entity {request.Entity}");
                    return;
                }

                var command = BroadcastRpcCommand.Write(sender.ID, request);

                var message = Room.Broadcast(ref command, mode: mode, channel: channel, group: request.Group, exception1: sender.ID, exception2: request.Exception);

                if (request.BufferMode != RemoteBufferMode.None)
                {
                    if (request.Group == NetworkGroupID.Default)
                        entity.RpcBuffer.Set(message, ref request, request.BufferMode, MessageBuffer.Add, MessageBuffer.RemoveAll);
                    else
                        Log.Warning($"Client {sender} Requesting to Buffer RPC Sent to None Default Channel, This is not Supported");
                }
            }

            void InvokeTargetRPC(NetworkClient sender, ref TargetRpcRequest request, DeliveryMode mode, byte channel)
            {
                if (Entities.TryGet(request.Entity, out var entity) == false)
                {
                    Log.Warning($"Client {sender.ID} Trying to Invoke RPC {request.Method} On Unregisterd Entity {request.Entity}");
                    return;
                }

                if (Clients.TryGet(request.Target, out var target) == false)
                {
                    Log.Warning($"No NetworkClient With ID {request.Target} Found to Send RPC {request.Method} To");
                    return;
                }

                var command = TargetRpcCommand.Write(sender.ID, request);

                Room.Send(ref command, target, mode: mode, channel: channel);
            }

            void InvokeQueryRPC(NetworkClient sender, ref QueryRpcRequest request, DeliveryMode mode, byte channel)
            {
                if (Entities.TryGet(request.Entity, out var entity) == false)
                {
                    Log.Warning($"Client {sender.ID} Trying to Invoke RPC {request.Method} On Unregisterd Entity {request.Entity}");
                    return;
                }

                if (Clients.TryGet(request.Target, out var target) == false)
                {
                    Log.Warning($"No NetworkClient With ID {request.Target} Found to Send RPC {request.Method} To");
                    ResolveRPR(sender, ref request, RemoteResponseType.InvalidClient);
                    return;
                }

                var command = QueryRpcCommand.Write(sender.ID, request);

                Room.Send(ref command, target, mode: mode, channel: channel);
            }

            void InvokeBufferRPC(NetworkClient sender, ref BufferRpcRequest request)
            {
                if (request.BufferMode == RemoteBufferMode.None)
                {
                    Log.Warning($"Recived Buffer RPC with Mode {request.BufferMode} Isn't Valid for Buffering!");
                    return;
                }

                if (Entities.TryGet(request.Entity, out var entity) == false)
                {
                    Log.Warning($"Client {sender.ID} Trying to Invoke RPC {request.Method} On Unregisterd Entity {request.Entity}");
                    return;
                }

                var command = BufferRpcCommand.Write(sender.ID, request);

                var message = NetworkMessage.Write(ref command);

                entity.RpcBuffer.Set(message, ref request, request.BufferMode, MessageBuffer.Add, MessageBuffer.RemoveAll);
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

            void ResolveRPR(NetworkClient requester, ref QueryRpcRequest request, RemoteResponseType response)
            {
                var command = RprCommand.Write(request.Channel, response);
                Room.Send(ref command, requester);
            }
            #endregion

            #region Sync Var
            void InvokeBroadcastSyncVar(NetworkClient sender, ref BroadcastSyncVarRequest request, DeliveryMode mode, byte channel)
            {
                if (Entities.TryGet(request.Entity, out var entity) == false)
                {
                    Log.Warning($"Client {sender} Trying to Invoke SyncVar on Non Existing Entity {request.Entity}");
                    return;
                }

                var command = SyncVarCommand.Write(sender.ID, request);

                var message = Room.Broadcast(ref command, mode: mode, channel: channel, group: request.Group, exception1: sender.ID);

                entity.SyncVarBuffer.Set(message, ref request, MessageBuffer.Add, MessageBuffer.Remove);
            }

            void InvokeBufferSyncVar(NetworkClient sender, ref BufferSyncVarRequest request)
            {
                if (Entities.TryGet(request.Entity, out var entity) == false)
                {
                    Log.Warning($"Client {sender} Trying to Invoke SyncVar on Non Existing Entity {request.Entity}");
                    return;
                }

                var command = SyncVarCommand.Write(sender.ID, request);

                var message = NetworkMessage.Write(ref command);

                entity.SyncVarBuffer.Set(message, ref request, MessageBuffer.Add, MessageBuffer.Remove);
            }
            #endregion
        }

        public MessageBufferProperty MessageBuffer = new MessageBufferProperty();
        public class MessageBufferProperty : Property
        {
            public List<NetworkMessage> List { get; protected set; }

            public NetworkMessage[] ToArray() => List.ToArray();

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
            public delegate void MessageCallbackDelegate(NetworkClient sender, NetworkMessage message, DeliveryMode mode, byte channel);

            public void Invoke(NetworkClient sender, NetworkMessage message, DeliveryMode mode, byte channel)
            {
                if (Dictionary.TryGetValue(message.Type, out var callback))
                    callback(sender, message, mode, channel);
                else
                    Log.Warning($"No Message Handler Registered for Payload {message.Type}");
            }

            public delegate void MessageHandler1Delegate<TPayload>(NetworkClient sender, ref TPayload payload, DeliveryMode mode, byte channel);
            public void RegisterHandler<TPayload>(MessageHandler1Delegate<TPayload> handler)
            {
                var type = typeof(TPayload);

                RegisterHandler(type, Callback);

                void Callback(NetworkClient sender, NetworkMessage message, DeliveryMode mode, byte channel)
                {
                    var payload = message.Read<TPayload>();

                    handler.Invoke(sender, ref payload, mode, channel);
                }
            }

            public delegate void MessageHandler2Delegate<TPayload>(NetworkClient sender, ref TPayload payload);
            public void RegisterHandler<TPayload>(MessageHandler2Delegate<TPayload> handler)
            {
                var type = typeof(TPayload);

                RegisterHandler(type, Callback);

                void Callback(NetworkClient sender, NetworkMessage message, DeliveryMode mode, byte channel)
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

            public MessageDispatcherProperty()
            {
                Dictionary = new Dictionary<Type, MessageCallbackDelegate>();
            }
        }

        public SendQueueProperty SendQueue = new SendQueueProperty();
        public class SendQueueProperty : Property
        {
            HashSet<NetworkClient> hash;

            public void Add(byte[] message, NetworkClient target, DeliveryMode mode, byte channel)
            {
                target.SendQueue.Add(message, mode, channel);

                hash.Add(target);
            }

            public void Remove(NetworkClient client) => hash.Remove(client);

            public void Resolve()
            {
                foreach (var client in hash)
                    foreach (var packet in client.SendQueue.Iterate())
                        TransportContext.Send(client.ID, packet.raw, packet.delivery, packet.channel);

                hash.Clear();
            }

            public SendQueueProperty()
            {
                hash = new HashSet<NetworkClient>();
            }
        }

        void ForAllProperties(Action<Property> action)
        {
            action(Info);
            action(Time);
            action(Clients);
            action(Master);
            action(Groups);
            action(Scenes);
            action(Entities);
            action(RemoteCalls);
            action(MessageBuffer);
            action(MessageDispatcher);
            action(SendQueue);
            action(System);
        }

        public class Property
        {
            public Room Room;
            public virtual void Set(Room reference) => Room = reference;

            #region Properties
            public InfoProperty Info => Room.Info;

            public ClientsProperty Clients => Room.Clients;

            public MasterProperty Master => Room.Master;

            public EntitiesProperty Entities => Room.Entities;

            public ScenesProperty Scenes => Room.Scenes;

            public MessageDispatcherProperty MessageDispatcher => Room.MessageDispatcher;

            public MessageBufferProperty MessageBuffer => Room.MessageBuffer;

            public SendQueueProperty SendQueue => Room.SendQueue;

            public TimeProperty Time => Room.Time;

            public SystemProperty System => Room.System;
            #endregion

            public INetworkTransportContext TransportContext => Room.TransportContext;

            public virtual void Configure() { }

            public virtual void Start() { }
        }

        Scheduler Scheduler;

        public bool IsRunning => Scheduler.IsRunning;

        public INetworkTransportContext TransportContext;

        #region Communication
        NetworkMessage Send<T>(ref T payload, NetworkClient target, DeliveryMode mode = DeliveryMode.ReliableOrdered, byte channel = 0)
        {
            var message = NetworkMessage.Write(ref payload);

            var raw = NetworkSerializer.Serialize(message);

            SendQueue.Add(raw, target, mode, channel);

            return message;
        }

        NetworkMessage Broadcast<T>(ref T payload, DeliveryMode mode = DeliveryMode.ReliableOrdered, byte channel = 0, NetworkGroupID group = default, NetworkClientID? exception1 = null, NetworkClientID? exception2 = null)
        {
            var message = NetworkMessage.Write(ref payload);

            var raw = NetworkSerializer.Serialize(message);

            for (int i = 0; i < Clients.Count; i++)
            {
                if (Clients[i].Groups.Contains(group) == false) continue;
                if (exception1 == Clients[i].ID) continue;
                if (exception2 == Clients[i].ID) continue;

                SendQueue.Add(raw, Clients[i], mode, channel);
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

            OnTick += ClearEmptyRoomProcedure;
        }

        void ClearEmptyRoomProcedure()
        {
            if (Time.Span.Seconds > 20)
            {
                OnTick -= ClearEmptyRoomProcedure;

                if (Occupancy == 0) Stop();
            }
        }

        public event Action OnTick;
        void Tick()
        {
            Time.Calculate();

            TransportContext.Poll();

            if (Scheduler.IsRunning) SendQueue.Resolve();

            OnTick?.Invoke();
        }

        void MessageRecievedCallback(NetworkClientID id, NetworkMessage message, DeliveryMode mode, byte channel)
        {
            if (Clients.TryGet(id, out var sender))
            {
                MessageDispatcher.Invoke(sender, message, mode, channel);
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

        public Room(RoomID id,
            AppConfig app,
            Version version,
            string name,
            byte capacity,
            bool visible,
            string password,
            MigrationPolicy migrationPolicy,
            AttributesCollection attributes)
        {
            this.ID = id;

            this.Version = version;
            this.App = app;

            this.Name = name;
            this.Capacity = capacity;
            this.Visible = visible;
            this.Password = password;
            this.MigrationPolicy = migrationPolicy;
            this.Attributes = attributes;

            Scheduler = new Scheduler(App.TickDelay, Tick);

            ForAllProperties(x => x.Set(this));
            ForAllProperties(x => x.Configure());
        }
    }
}