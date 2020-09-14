using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Server;

using System.Threading;

using System.Collections.Concurrent;

namespace Backend
{
    class Room
    {
        public ushort ID { get; protected set; }

        public string Path => "/" + ID;

        public string Name { get; protected set; }

        public ushort Capacity { get; protected set; }

        public int PlayersCount => Clients.Count;

        public AttributesCollection Attributes { get; protected set; }

        #region Read Info
        public RoomBasicInfo ReadBasicInfo() => new RoomBasicInfo(ID, Name, Capacity, PlayersCount, Attributes);

        public NetworkClientInfo[] GetClientsInfo()
        {
            var list = new NetworkClientInfo[Clients.Count];

            var index = 0;

            foreach (var client in Clients.Collection)
            {
                list[index] = client.ReadInfo();

                index += 1;
            }

            return list;
        }

        public RoomInternalInfo ReadInternalInfo()
        {
            var info = new RoomInternalInfo();

            return info;
        }
        #endregion

        #region Web Socket
        public WebSocketServiceHost WebSocket { get; protected set; }

        public class WebSocketService : WebSocketBehavior
        {
            public Room Room { get; protected set; }
            public void Set(Room reference) => Room = reference;

            public ActionQueue InputQueue => Room.InputQueue;

            protected override void OnOpen()
            {
                base.OnOpen();

                InputQueue.Enqueue(Invoke);

                void Invoke() => Room.ClientConnected(this.ID);
            }

            protected override void OnMessage(MessageEventArgs args)
            {
                base.OnMessage(args);

                var message = NetworkMessage.Read(args.RawData);

                InputQueue.Enqueue(Invoke);

                void Invoke() => Room.ClientMessageCallback(this.ID, args.RawData, message);
            }

            protected override void OnClose(CloseEventArgs args)
            {
                base.OnClose(args);

                InputQueue.Enqueue(Invoke);

                void Invoke() => Room.ClientDisconnected(this.ID);
            }

            public WebSocketService()
            {

            }
        }

        public void InitializeService(WebSocketService service) => service.Set(this);

        public Dictionary<string, NetworkClient> WebSocketClients { get; protected set; }
        #endregion

        #region Schedule
        Schedule schedule;

        public const long DefaultTickInterval = 50;
        #endregion

        public IDCollection<NetworkClient> Clients { get; protected set; }

        public IDCollection<NetworkEntity> Entities { get; protected set; }

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

            Broadcast(command);
        }

        void ChangeMaster()
        {
            if (Clients.Count > 0)
            {
                var target = Clients.Collection.First();

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

        public void UnbufferMessages(NetworkMessageCollection collection) => MessageBuffer.RemoveAll(x => collection.Contains(x));
        #endregion

        public ActionQueue InputQueue { get; protected set; }

        #region Communication
        NetworkMessage SendTo<T>(NetworkClient client, T payload) => SendTo(client.WebsocketID, payload);
        NetworkMessage SendTo<T>(string websocketID, T payload)
        {
            var message = NetworkMessage.Write(payload);

            var binary = NetworkSerializer.Serialize(message);

            WebSocket.Sessions.SendTo(binary, websocketID);

            return message;
        }

        NetworkMessage Broadcast<T>(T payload)
        {
            var message = NetworkMessage.Write(payload);

            var binary = NetworkSerializer.Serialize(message);

            //Log.Info($"{typeof(T)} Binary Size: {binary.Length}");

            WebSocket.Sessions.Broadcast(binary);

            return message;
        }
        #endregion

        public void Start()
        {
            Log.Info($"Starting Room {ID}");

            GameServer.WebSocket.AddService<WebSocketService>(Path, InitializeService);
            WebSocket = GameServer.WebSocket.Services[Path];

            schedule.Start();
        }

        public event Action OnTick;
        void Tick()
        {
            //Log.Info($"Delta Time: {Schedule.DeltaTime}\n" + $"Processing: {InputQueue.Count}");

            while (InputQueue.Dequeue(out var callback))
                callback();

            OnTick?.Invoke();
        }

        void ClientConnected(string websocketID)
        {
            Log.Info($"Room {this.ID}: Client {websocketID} Connected");
        }

        #region Client Messages
        void ClientMessageCallback(string websocketID, byte[] raw, NetworkMessage message)
        {
            //Log.Info($"{message.Type} Binary Size: {raw.Length}");

            if(WebSocketClients.TryGetValue(websocketID, out var client))
            {
                if (message.Is<RpcRequest>())
                {
                    var request = message.Read<RpcRequest>();

                    InvokeRPC(client, request);
                }
                else if (message.Is<SpawnEntityRequest>())
                {
                    var request = message.Read<SpawnEntityRequest>();

                    SpawnEntity(client, request);
                }
                else if(message.Is<DestroyEntityRequest>())
                {
                    var request = message.Read<DestroyEntityRequest>();

                    DestroyEntity(client, request);
                }
                else if(message.Is<RprRequest>())
                {
                    var callback = message.Read<RprRequest>();

                    InvokeRPR(client, callback);
                }
                else if (message.Is<ReadyClientRequest>())
                {
                    var request = message.Read<ReadyClientRequest>();

                    ReadyClient(client);
                }
            }
            else
            {
                if (message.Is<RegisterClientRequest>())
                {
                    var request = message.Read<RegisterClientRequest>();

                    RegisterClient(websocketID, request);
                }
            }
        }

        void RegisterClient(string websocketID, RegisterClientRequest request) => RegisterClient(websocketID, request.Profile);
        void RegisterClient(string websocketID, NetworkClientProfile profile)
        {
            if (WebSocketClients.ContainsKey(websocketID))
            {
                Log.Warning($"Client {websocketID} Already Registered With Room {this.ID}, Ignoring Register Request");
                return;
            }

            if(WebSocket.Sessions.TryGetSession(websocketID, out var session) == false) //TODO remember why I needed this?
            {
                Log.Warning($"No WebSocket Session Found for Client {websocketID}, Ignoring Register Request");
                return;
            }

            var code = Clients.Reserve();
            var id = new NetworkClientID(code);

            var info = new NetworkClientInfo(id, profile);

            var client = new NetworkClient(info, session);

            if (Clients.Count == 0) SetMaster(client);

            Clients.Assign(client, code);
            WebSocketClients.Add(websocketID, client);

            Log.Info($"Room {this.ID}: Client {websocketID} Registerd as Client {id}");

            var room = ReadInternalInfo();
            var response = new RegisterClientResponse(id, room);
            SendTo(client, response);

            var payload = new ClientConnectedPayload(info);
            Broadcast(payload);
        }

        void ReadyClient(NetworkClient client)
        {
            Log.Info($"Room {this.ID}: Client {client.ID} Set Ready");

            client.Ready();

            var response = new ReadyClientResponse(GetClientsInfo(), Master.ID, MessageBuffer.List);

            SendTo(client, response);
        }

        void InvokeRPC(NetworkClient sender, RpcRequest request)
        {
            if (Entities.TryGetValue(request.Entity.Value, out var entity))
                InvokeRPC(sender, entity, request);
            else
                Log.Warning($"Client {sender.ID} Trying to Invoke RPC {request.Method} On Unregisterd Entity {request.Entity}");
        }
        void InvokeRPC(NetworkClient sender, NetworkEntity entity, RpcRequest request)
        {
            var command = RpcCommand.Write(sender.ID, request);

            if (request.Type == RpcType.Broadcast)
            {
                var message = Broadcast(command);

                entity.RpcBuffer.Set(message, request, BufferMessage, UnbufferMessages);
            }

            if (request.Type == RpcType.Target || request.Type == RpcType.Callback)
            {
                if (Clients.TryGetValue(request.Target.Value, out var client))
                {
                    SendTo(client, command);

                    if (request.Type == RpcType.Callback) entity.RprCache.Register(request, sender);
                }
                else
                {
                    Log.Warning($"No NetworkClient With ID {request.Target} Found to Send RPC {request.Method} To");
                }
            }
        }

        void InvokeRPR(NetworkClient sender, RprRequest request)
        {
            if (Entities.TryGetValue(request.Entity.Value, out var entity) == false)
            {
                Log.Warning($"No Entity {request.Entity} Found to Invoke RPC Callback On");
                return;
            }
            
            if (Clients.TryGetValue(request.Target.Value, out var target) == false)
            {
                Log.Warning($"No Client {request.Target} Found to Invoke RPC Callback On");
                return;
            }

            if (sender != entity.Owner)
            {
                Log.Warning($"Client {sender.Name} Trying to Invoke RPC Callback on Entity {entity.ID} Without Having Ownership of that Entity");
                return;
            }

            entity.RprCache.Unregister(request.Callback);

            var command = RprCommand.Write(entity.ID, request.Callback, request.Raw);

            SendTo(target, command);
        }

        void ResolveRprCache(NetworkEntity entity)
        {
            foreach (var callback in entity.RprCache.Collection)
            {
                var command = RprCommand.Write(entity.ID, callback.ID, false);

                SendTo(callback.Sender, command);
            }
        }

        NetworkEntity SpawnEntity(NetworkClient sender, SpawnEntityRequest request)
        {
            if(request.Type == NetworkEntityType.SceneObject && sender != Master)
            {
                Log.Warning($"Non Master Client {sender.ID} Trying to Spawn Scene Object");
                return null;
            }

            var code = Entities.Reserve();
            var id = new NetworkEntityID(code);

            var entity = new NetworkEntity(sender, id, request.Type);

            sender.Entities.Add(entity);
            Entities.Assign(entity, code);

            if (request.Type == NetworkEntityType.SceneObject) SceneObjects.Add(entity);

            var command = SpawnEntityCommand.Write(sender.ID, entity.ID, request);

            var message = Broadcast(command);

            entity.SpawnMessage = message;
            BufferMessage(message);

            return entity;
        }

        void DestroyEntity(NetworkClient sender, DestroyEntityRequest request)
        {
            if (Entities.TryGetValue(request.ID.Value, out var entity) == false)
            {
                Log.Warning($"Client {sender} Trying to Destory Non Registered Entity {request.ID}");
                return;
            }

            if (sender != entity.Owner && sender != Master)
            {
                Log.Warning($"Client {sender} Trying to Destory Entity {entity} Without Having Authority on that Entity");
                return;
            }

            DestroyEntity(entity);
        }
        #endregion

        void ClientDisconnected(string websocketID)
        {
            Log.Info($"Room {this.ID}: Client {websocketID} Disconnected");

            if (WebSocketClients.TryGetValue(websocketID, out var client)) RemoveClient(client);
        }

        void DestroyEntity(NetworkEntity entity)
        {
            var owner = entity.Owner;

            UnbufferMessage(entity.SpawnMessage);

            entity.RpcBuffer.Clear(UnbufferMessages);
            ResolveRprCache(entity);

            Entities.Remove(entity);
            owner.Entities.Remove(entity);

            var command = new DestroyEntityCommand(entity.ID);

            Broadcast(command);
        }

        void RemoveClient(NetworkClient client)
        {
            for (int i = client.Entities.Count; i-- > 0;)
                DestroyEntity(client.Entities[i]);

            WebSocketClients.Remove(client.WebsocketID);
            Clients.Remove(client);

            if (client == Master) ChangeMaster();

            var payload = new ClientDisconnectPayload(client.ID);
            Broadcast(payload);
        }

        public void Stop()
        {
            Log.Info($"Stopping Room: {ID}");

            schedule.Stop();

            GameServer.WebSocket.RemoveService(Path);
        }

        public Room(ushort id, string name, ushort capacity, AttributesCollection attributes)
        {
            this.ID = id;
            this.Name = name;
            this.Capacity = capacity;
            this.Attributes = attributes;

            WebSocketClients = new Dictionary<string, NetworkClient>();

            MessageBuffer = new NetworkMessageCollection();

            Clients = new IDCollection<NetworkClient>();
            Entities = new IDCollection<NetworkEntity>();

            SceneObjects = new List<NetworkEntity>();

            InputQueue = new ActionQueue();

            schedule = new Schedule(DefaultTickInterval, Tick);
        }
    }
}