using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Server;

using System.Threading;

using Game.Shared;

using System.Collections.Concurrent;

namespace Game.Server
{
    class Room
    {
        #region Basic Properties
        public ushort ID { get; protected set; }

        public string Path => "/" + ID;

        public string Name { get; protected set; }

        public ushort Capacity { get; protected set; }

        public int PlayersCount => Clients.Count;

        public AttributesCollection Attributes { get; protected set; }

        public RoomBasicInfo ReadBasicInfo() => new RoomBasicInfo(ID, Name, Capacity, PlayersCount, Attributes);
        #endregion

        #region Internal Properties
        public RoomInternalInfo ReadInternalInfo()
        {
            var info = new RoomInternalInfo();

            return info;
        }

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
        #endregion

        #region Web Socket
        public WebSocketServiceHost WebSocket { get; protected set; }

        public class WebSocketService : WebSocketBehavior
        {
            public Room Room { get; protected set; }
            public void Set(Room reference) => Room = reference;

            public ActionQueue ActionQueue => Room.InputQueue;

            protected override void OnOpen()
            {
                base.OnOpen();

                ActionQueue.Enqueue(Invoke);

                void Invoke() => Room.ClientConnected(this.ID);
            }

            protected override void OnMessage(MessageEventArgs args)
            {
                base.OnMessage(args);

                var message = NetworkMessage.Read(args.RawData);

                ActionQueue.Enqueue(Invoke);

                void Invoke() => Room.ClientMessageCallback(this.ID, args.RawData, message);
            }

            protected override void OnClose(CloseEventArgs args)
            {
                base.OnClose(args);

                ActionQueue.Enqueue(Invoke);

                void Invoke() => Room.ClientDisconnected(this.ID);
            }

            public WebSocketService()
            {

            }
        }

        public void InitializeService(WebSocketService service)
        {
            service.Set(this);
        }

        public Dictionary<string, NetworkClient> WebSocketClients { get; protected set; }
        #endregion

        #region Schedule
        public Schedule Schedule { get; protected set; }

        public const long DefaultTickInterval = 50;
        #endregion

        public IDCollection<NetworkClient> Clients { get; protected set; }

        public IDCollection<NetworkEntity> Entities { get; protected set; }

        #region Message Buffer
        public List<NetworkMessage> MessageBuffer { get; protected set; }

        public void BufferMessage(NetworkMessage message)
        {
            MessageBuffer.Add(message);
        }

        public void UnbufferMessage(NetworkMessage message)
        {
            MessageBuffer.Remove(message);
        }
        public void UnbufferMessages(IList<NetworkMessage> list)
        {
            MessageBuffer.RemoveAll(Contained);

            bool Contained(NetworkMessage message) => list.Contains(message);
        }
        #endregion

        public ActionQueue InputQueue { get; protected set; }

        #region Communication
        NetworkMessage SendTo<T>(NetworkClient client, T payload)
        {
            var message = NetworkMessage.Write(payload);

            var binary = NetworkSerializer.Serialize(message);

            WebSocket.Sessions.SendToAsync(binary, client.WebsocketID, null);

            return message;
        }

        NetworkMessage Broadcast<T>(T payload)
        {
            var message = NetworkMessage.Write(payload);

            var binary = NetworkSerializer.Serialize(message);

            Log.Info($"{typeof(T)} Binary Size: {binary.Length}");

            WebSocket.Sessions.BroadcastAsync(binary, null);

            return message;
        }
        #endregion

        public void Start()
        {
            Log.Info($"Starting Room {ID}");

            GameServer.WebSocket.AddService<WebSocketService>(Path, InitializeService);
            WebSocket = GameServer.WebSocket.Services[Path];

            Schedule.Start();
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

        #region Messages
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

            var client = new NetworkClient(id, session, profile);

            Clients.Assign(client, code);
            WebSocketClients.Add(websocketID, client);

            Log.Info($"Room {this.ID}: Client {websocketID} Registerd");

            var info = ReadInternalInfo();
            var response = new RegisterClientResponse(id, info);
            SendTo(client, response);

            var payload = new ClientConnectedPayload(id, profile);
            Broadcast(payload);
        }

        void ReadyClient(NetworkClient client)
        {
            Log.Info($"Room {this.ID}: Client {client.ID} Set Ready");

            client.Ready();

            var response = new ReadyClientResponse(GetClientsInfo(), MessageBuffer);

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

            var message = Broadcast(command);

            entity.RPCBuffer.Set(message, request, UnbufferMessages);
        }

        void SpawnEntity(NetworkClient owner, SpawnEntityRequest request) => SpawnEntity(owner, request.Resource, request.Attributes);
        void SpawnEntity(NetworkClient owner, string resource, AttributesCollection attributes)
        {
            var code = Entities.Reserve();
            var id = new NetworkEntityID(code);

            var entity = new NetworkEntity(id);

            owner.Entities.Add(entity);
            Entities.Assign(entity, code);

            var command = new SpawnEntityCommand(owner.ID, entity.ID, resource, attributes);
            var message = Broadcast(command);

            entity.SpawnMessage = message;
            BufferMessage(message);
        }
        #endregion

        void ClientDisconnected(string websocketID)
        {
            Log.Info($"Room {this.ID}: Client {websocketID} Disconnected");

            if (WebSocketClients.TryGetValue(websocketID, out var client)) RemoveClient(client);
        }

        void RemoveClient(NetworkClient client)
        {
            foreach (var entity in client.Entities)
            {
                UnbufferMessage(entity.SpawnMessage);

                entity.RPCBuffer.UnBufferAll(UnbufferMessages);

                Entities.Remove(entity);
            }

            WebSocketClients.Remove(client.WebsocketID);
            Clients.Remove(client);

            var payload = new ClientDisconnectPayload(client.ID, client.Profile);
            Broadcast(payload);
        }

        public void Stop()
        {
            Log.Info($"Stopping Room: {ID}");

            Schedule.Stop();

            GameServer.WebSocket.RemoveService(Path);
        }

        public Room(ushort id, string name, ushort capacity, AttributesCollection attributes)
        {
            this.ID = id;
            this.Name = name;
            this.Capacity = capacity;
            this.Attributes = attributes;

            WebSocketClients = new Dictionary<string, NetworkClient>();

            MessageBuffer = new List<NetworkMessage>();

            Clients = new IDCollection<NetworkClient>();
            Entities = new IDCollection<NetworkEntity>();

            InputQueue = new ActionQueue();

            Schedule = new Schedule(DefaultTickInterval, Tick);
        }
    }
}