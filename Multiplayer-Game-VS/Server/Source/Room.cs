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

        public RoomBasicInfo ReadBasicInfo() => new RoomBasicInfo(ID, Name, Capacity, PlayersCount);
        #endregion

        #region Web Socket
        public WebSocketServiceHost WebSocket => GameServer.WebSocket.Services[Path];

        public class WebSocketService : WebSocketBehavior
        {
            public Room Room { get; protected set; }
            public void Set(Room reference) => Room = reference;

            public RoomActionQueue ActionQueue => Room.ActionQueue;

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

        NetworkMessage SendTo(NetworkClient client, INetworkMessagePayload payload) => SendTo(client.ID, payload);
        NetworkMessage SendTo(NetworkClientID id, INetworkMessagePayload payload)
        {
            var message = NetworkMessage.Write(payload);

            var binary = NetworkSerializer.Serialize(message);

            WebSocket.Sessions.SendTo(binary, id);

            return message;
        }

        NetworkMessage BroadcastToReady(INetworkMessagePayload payload)
        {
            var message = NetworkMessage.Write(payload);

            var binary = NetworkSerializer.Serialize(message);

            foreach (var client in Clients.Values)
            {
                if (IsActive(client.ID) == false) continue;

                if (client.IsReady)
                    WebSocket.Sessions.SendTo(binary, client.ID);
            }

            return message;
        }

        bool IsActive(NetworkClientID id) => WebSocket.Sessions.ActiveIDs.Contains(id.Value);
        #endregion

        #region Schedule
        public Schedule Schedule { get; protected set; }

        public const long DefaultTickInterval = 50;
        #endregion

        public RoomInternalInfo ReadInternalInfo()
        {
            var info = new RoomInternalInfo();

            return info;
        }

        public NetworkClientInfo[] GetClientsInfo()
        {
            var list = new NetworkClientInfo[Clients.Count];

            var index = 0;

            foreach (var client in Clients.Values)
            {
                list[index] = client.ReadInfo();

                index += 1;
            }

            return list;
        }

        public Dictionary<NetworkClientID, NetworkClient> Clients { get; protected set; }

        public Dictionary<NetworkEntityID, NetworkEntity> Entities { get; protected set; }

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

        public RoomActionQueue ActionQueue { get; protected set; }

        public void Start()
        {
            Log.Info($"Starting Room {ID}");

            Schedule.Start();
        }

        public event Action OnInit;
        void Init()
        {
            OnInit?.Invoke();
        }
        
        public event Action OnTick;
        void Tick()
        {
            OnTick?.Invoke();

            while (ActionQueue.Dequeue(out var callback))
                callback();
        }

        void ClientConnected(NetworkClientID id)
        {
            Log.Info($"Room {this.ID}: Client {id} Connected");
        }

        #region Messages
        void ClientMessageCallback(NetworkClientID id, byte[] raw, NetworkMessage message)
        {
            if (Clients.TryGetValue(id, out var client))
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

                    RegisterClient(id, request);
                }
            }
        }

        void RegisterClient(NetworkClientID id, RegisterClientRequest request) => RegisterClient(id, request.Profile);
        void RegisterClient(NetworkClientID id, NetworkClientProfile profile)
        {
            if (Clients.ContainsKey(id))
            {
                Log.Warning($"Client {id} Already Registered With Room {this.ID}, Ignoring Request");
                return;
            }

            Log.Info($"Room {this.ID}: Client {id} Registerd");

            var client = new NetworkClient(id, profile);

            Clients.Add(id, client);

            var info = ReadInternalInfo();
            var response = new RegisterClientResponse(id, info);
            SendTo(client, response);

            var payload = new ClientConnectedPayload(id, profile);
            BroadcastToReady(payload);
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
            if (Entities.TryGetValue(request.Entity, out var entity))
                InvokeRPC(sender, entity, request);
            else
                Log.Warning($"Client {sender.ID} Trying to Invoke RPC {request.Method} On Unregisterd Entity {request.Entity}");
        }
        void InvokeRPC(NetworkClient sender, NetworkEntity entity, RpcRequest request)
        {
            var command = RpcCommand.Write(sender.ID, request);

            var message = BroadcastToReady(command);

            entity.RPCBuffer.Set(message, request, UnbufferMessages);
        }

        void SpawnEntity(NetworkClient owner, SpawnEntityRequest request) => SpawnEntity(owner, request.Resource);
        void SpawnEntity(NetworkClient owner, string resource)
        {
            var id = NetworkEntityID.Generate();

            var entity = new NetworkEntity(id);

            owner.Entities.Add(entity);
            Entities.Add(id, entity);

            var command = SpawnEntityCommand.Write(owner.ID, entity.ID, resource);
            var message = BroadcastToReady(command);

            entity.SpawnMessage = message;
            BufferMessage(message);
        }
        #endregion

        void ClientDisconnected(NetworkClientID id)
        {
            Log.Info($"Room {this.ID}: Client {id} Disconnected");

            if (Clients.TryGetValue(id, out var client)) RemoveClient(client);
        }

        void RemoveClient(NetworkClient client)
        {
            foreach (var entity in client.Entities)
            {
                UnbufferMessage(entity.SpawnMessage);

                entity.RPCBuffer.UnBufferAll(UnbufferMessages);

                Entities.Remove(entity.ID);
            }

            Clients.Remove(client.ID);

            var payload = new ClientDisconnectPayload(client.ID, client.Profile);
            BroadcastToReady(payload);
        }

        public void Stop()
        {
            Log.Info($"Stopping Room: {ID}");

            Schedule.Stop();

            GameServer.WebSocket.RemoveService(Path);
        }

        public Room(ushort id, string name, ushort capacity)
        {
            this.ID = id;
            this.Name = name;
            this.Capacity = capacity;

            MessageBuffer = new List<NetworkMessage>();

            Clients = new Dictionary<NetworkClientID, NetworkClient>();

            Entities = new Dictionary<NetworkEntityID, NetworkEntity>();

            GameServer.WebSocket.AddService<WebSocketService>(Path, InitializeService);

            ActionQueue = new RoomActionQueue();

            Schedule = new Schedule(DefaultTickInterval, Init, Tick);
        }
    }

    class RoomActionQueue
    {
        public ConcurrentQueue<Callback> Collection { get; protected set; }

        public int Count => Collection.Count;

        public delegate void Callback();

        public void Enqueue(Callback callback)
        {
            Collection.Enqueue(callback);
        }

        public bool Dequeue(out Callback callback)
        {
            return Collection.TryDequeue(out callback);
        }

        public RoomActionQueue()
        {
            Collection = new ConcurrentQueue<Callback>();
        }
    }
}