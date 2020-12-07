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

        public AppID AppID { get; protected set; }

        public Version Version { get; protected set; }

        public string Name { get; protected set; }

        public byte Capacity { get; protected set; }
        public byte Occupancy => (byte)Clients.Count;

        public bool IsFull => Occupancy >= Capacity;

        public AttributesCollection Attributes { get; protected set; }

        #region Read Info
        public RoomBasicInfo GetBasicInfo() => new RoomBasicInfo(ID, Name, Capacity, Occupancy, Attributes);
        public static RoomBasicInfo GetBasicInfo(Room room) => room.GetBasicInfo();

        public RoomInnerInfo GetInnerInfo() => new RoomInnerInfo(TickLatency);
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

        public RealtimeAPI Realtime => GameServer.Realtime;

        public INetworkTransportContext TransportContext { get; protected set; }

        public Scheduler Scheduler { get; protected set; }

        public byte TickLatency { get; protected set; }

        public DateTime Timestamp { get; protected set; }

        public NetworkTimeSpan Time { get; protected set; }

        public Dictionary<NetworkClientID, NetworkClient> Clients { get; protected set; }

        public AutoKeyDictionary<NetworkEntityID, NetworkEntity> Entities { get; protected set; }

        public List<NetworkEntity> SceneObjects { get; protected set; }

        #region Master
        public NetworkClient Master { get; protected set; }

        void SetMaster(NetworkClient target)
        {
            Master = target;

            for (int i = 0; i < SceneObjects.Count; i++)
                SceneObjects[i].SetOwner(Master);
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
        public NetworkMessageCollection MessageBuffer { get; protected set; }

        public void BufferMessage(NetworkMessage message) => MessageBuffer.Add(message);

        public void UnbufferMessage(NetworkMessage message) => MessageBuffer.Remove(message);

        public void UnbufferMessages(HashSet<NetworkMessage> collection) => MessageBuffer.RemoveAll(x => collection.Contains(x));
        #endregion

        #region Communication

        #region Send
        NetworkMessage Send<T>(T payload, NetworkClient target, DeliveryMode mode = DeliveryMode.Reliable)
        {
            var message = NetworkMessage.Write(payload);

            if (Realtime.QueueMessages)
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

        void QueueMessage(NetworkMessage message, NetworkClient target, DeliveryMode mode) => target.SendQueue.Add(message, mode);
        #endregion

        #region Broadcast
        NetworkMessage Broadcast<T>(T payload, DeliveryMode mode = DeliveryMode.Reliable, NetworkClientID? exception = null, BroadcastCondition condition = null)
        {
            var message = NetworkMessage.Write(payload);

            if (Realtime.QueueMessages)
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

        void ResolveSendQueues()
        {
            foreach (var client in Clients.Values)
            {
                foreach (var delivery in client.SendQueue.Deliveries)
                {
                    if (delivery.Empty) continue;

                    foreach (var binary in delivery.Serialize(Realtime.Transport.MTU))
                        TransportContext.Send(client.ID, binary, delivery.Mode);
                }
            }
        }
        #endregion

        public void Start()
        {
            Log.Info($"Starting Room {ID}");

            Timestamp = DateTime.UtcNow;

            TransportContext = Realtime.Register(ID.Value);

            TransportContext.OnConnect += ClientConnected;
            TransportContext.OnMessage += MessageRecievedCallback;
            TransportContext.OnDisconnect += ClientDisconnected;

            Scheduler.Start();
        }

        void ClientConnected(NetworkClientID id)
        {
            Log.Info($"Room {this.ID}: Client {id} Connected");
        }

        public event Action OnTick;
        void Tick()
        {
            Time = NetworkTimeSpan.Calculate(Timestamp);

            TransportContext.Poll();

            OnTick?.Invoke();

            if (Scheduler.Running == false) return;

            if (Realtime.QueueMessages) ResolveSendQueues();
        }

        void MessageRecievedCallback(NetworkClientID id, NetworkMessage message, DeliveryMode mode)
        {
            //Log.Info($"{message.Type} Binary Size: {raw.Count}");

            if (Clients.TryGetValue(id, out var client))
            {
                if (message.Is<RpcRequest>())
                {
                    var request = message.Read<RpcRequest>();

                    InvokeRPC(client, request, mode);
                }
                else if (message.Is<SpawnEntityRequest>())
                {
                    var request = message.Read<SpawnEntityRequest>();

                    SpawnEntity(client, request);
                }
                else if (message.Is<DestroyEntityRequest>())
                {
                    var request = message.Read<DestroyEntityRequest>();

                    DestroyEntity(client, request);
                }
                else if (message.Is<RprRequest>())
                {
                    var callback = message.Read<RprRequest>();

                    InvokeRPR(client, callback);
                }
                else if (message.Is<SyncVarRequest>())
                {
                    var request = message.Read<SyncVarRequest>();

                    InvokeSyncVar(client, request, mode);
                }
                else if (message.Is<ReadyClientRequest>())
                {
                    var request = message.Read<ReadyClientRequest>();

                    ReadyClient(client, request);
                }
                else if (message.Is<RoomTimeRequest>())
                {
                    var request = message.Read<RoomTimeRequest>();

                    ProcessTimeRequest(client, request);
                }
                else if (message.Is<PingRequest>())
                {
                    var request = message.Read<PingRequest>();

                    ProcessPingRequest(client, request);
                }
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

            var time = new RoomTimeResponse(Time, request.Timestamp);

            var response = new ReadyClientResponse(GetClientsInfo(), Master.ID, MessageBuffer.List, time);

            Send(response, client);

            Log.Info($"Room {this.ID}: Client {client.ID} Set Ready");
        }

        #region RPC
        void InvokeRPC(NetworkClient sender, RpcRequest request, DeliveryMode mode)
        {
            if (Entities.TryGetValue(request.Entity, out var entity) == false)
            {
                Log.Warning($"Client {sender.ID} Trying to Invoke RPC {request.Method} On Unregisterd Entity {request.Entity}");

                if (request.Type == RpcType.Return) ResolveRpr(sender, request, RprResult.InvalidClient);

                return;
            }

            switch (request.Type)
            {
                case RpcType.Broadcast:
                    InvokeBroadcastRPC(sender, request, entity, mode);
                    break;

                case RpcType.Target:
                case RpcType.Return:
                    InvokeTargetedRPC(sender, request, entity, mode);
                    break;
            }
        }

        void InvokeBroadcastRPC(NetworkClient sender, RpcRequest request, NetworkEntity entity, DeliveryMode mode)
        {
            var command = RpcCommand.Write(sender.ID, request, Time);

            var message = Broadcast(command, mode: mode, condition: NetworkClient.IsReady, exception: request.Exception);

            entity.RpcBuffer.Set(message, request, BufferMessage, UnbufferMessages);
        }

        void InvokeTargetedRPC(NetworkClient sender, RpcRequest request, NetworkEntity entity, DeliveryMode mode)
        {
            var command = RpcCommand.Write(sender.ID, request, Time);

            if (Clients.TryGetValue(request.Target, out var target) == false)
            {
                Log.Warning($"No NetworkClient With ID {request.Target} Found to Send RPC {request.Method} To");

                if (request.Type == RpcType.Return) ResolveRpr(sender, request, RprResult.InvalidClient);

                return;
            }

            if (request.Type == RpcType.Return) entity.RprCache.Register(request, sender, target);

            Send(command, target, mode);
        }
        #endregion

        #region RPR
        void InvokeRPR(NetworkClient sender, RprRequest request)
        {
            if (Entities.TryGetValue(request.Entity, out var entity) == false)
            {
                Log.Warning($"No Entity '{request.Entity}' Found to Invoke RPR On");
                return;
            }

            if (Clients.TryGetValue(request.Target, out var target) == false)
            {
                Log.Warning($"No Client '{request.Target}' Found to Invoke RPR On");
                return;
            }

            if (entity.RprCache.TryGet(request, out var callback) == false)
            {
                Log.Warning($"No RPR '{request.ID}' Found in '{entity}'s RPR Cache");
                return;
            }

            if (sender != callback.Target)
            {
                Log.Info($"Client {sender} Sending RPR for Client {callback.Sender} Even Thought They Aren't the RPR Callback Target");
                return;
            }

            if (entity.RprCache.Unregister(request) == false)
                Log.Warning($"Couldn't Unregister Cached RPR {callback.ID}");

            var command = RprCommand.Write(entity.ID, request);

            Send(command, target);
        }

        void ResolveRpr(NetworkClient target, RpcRequest request, RprResult result) => ResolveRpr(target, request.Entity, request.Callback, result);
        void ResolveRpr(NetworkClient target, NetworkEntityID entity, ushort id, RprResult result)
        {
            var command = RprCommand.Write(entity, id, result);

            Send(command, target);
        }

        void ResolveRprCache(NetworkEntity entity, RprResult result)
        {
            foreach (var callback in entity.RprCache.Collection)
                ResolveRpr(callback.Sender, callback.Request, result);
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
            var response = new RoomTimeResponse(Time, request.Timestamp);

            Send(response, sender);
        }

        void ProcessPingRequest(NetworkClient sender, PingRequest request)
        {
            var response = new PingResponse(request);

            Send(response, sender);
        }

        NetworkEntity SpawnEntity(NetworkClient sender, SpawnEntityRequest request)
        {
            if (request.Type == NetworkEntityType.SceneObject && sender != Master)
            {
                Log.Warning($"Non Master Client {sender.ID} Trying to Spawn Scene Object");
                return null;
            }

            if (request.Owner != null && sender != Master)
            {
                Log.Warning($"Non Master Client {sender.ID} Trying to Spawn Object for Client {request.Owner}");
                return null;
            }

            var id = Entities.Reserve();

            NetworkClient owner;

            if (request.Owner.HasValue)
                Clients.TryGetValue(request.Owner.Value, out owner);
            else
                owner = sender;

            if (owner == null)
            {
                Log.Warning($"No Owner Found For Spawn Request");
                return null;
            }

            var entity = new NetworkEntity(owner, id, request.Type);

            owner.Entities.Add(entity);
            Entities.Assign(id, entity);

            if (request.Type == NetworkEntityType.SceneObject) SceneObjects.Add(entity);

            var command = SpawnEntityCommand.Write(owner.ID, entity.ID, request);

            var message = Broadcast(command, condition: NetworkClient.IsReady);

            entity.SpawnMessage = message;
            BufferMessage(message);

            return entity;
        }

        #region Destroy Entity
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

            DestroyEntity(entity);
        }

        void DestroyEntity(NetworkEntity entity)
        {
            var owner = entity.Owner;

            UnbufferMessage(entity.SpawnMessage);

            entity.RpcBuffer.Clear(UnbufferMessages);
            ResolveRprCache(entity, RprResult.Disconnected);
            entity.SyncVarBuffer.Clear(UnbufferMessages);

            Entities.Remove(entity.ID);
            owner.Entities.Remove(entity);

            var command = new DestroyEntityCommand(entity.ID);

            Broadcast(command, condition: NetworkClient.IsReady);
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
            for (int i = client.Entities.Count; i-- > 0;)
            {
                if (client.Entities[i].Type == NetworkEntityType.SceneObject) continue;

                DestroyEntity(client.Entities[i]);
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

            Scheduler.Stop();

            GameServer.Realtime.Unregister(ID.Value);

            OnStop?.Invoke(this);
        }

        public Room(RoomID id, AppID appID, Version version, string name, byte capacity, AttributesCollection attributes)
        {
            this.ID = id;

            this.Version = version;
            this.AppID = appID;

            this.Name = name;

            this.Capacity = capacity;

            this.Attributes = attributes;

            MessageBuffer = new NetworkMessageCollection();

            Clients = new Dictionary<NetworkClientID, NetworkClient>();
            Entities = new AutoKeyDictionary<NetworkEntityID, NetworkEntity>(NetworkEntityID.Increment);
            SceneObjects = new List<NetworkEntity>();

            TickLatency = 30;
            Scheduler = new Scheduler(TickLatency, Tick);
        }
    }
}