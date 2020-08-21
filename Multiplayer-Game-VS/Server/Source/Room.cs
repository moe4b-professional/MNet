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

        void SendTo(NetworkClient client, object payload) => SendTo(client.ID, payload);
        void SendTo(NetworkClientID id, object payload)
        {
            var message = NetworkMessage.Write(payload);

            var binary = NetworkSerializer.Serialize(message);

            WebSocket.Sessions.SendTo(binary, id);
        }

        void Broadcast(object payload)
        {
            var message = NetworkMessage.Write(payload);

            var binary = NetworkSerializer.Serialize(message);

            foreach (var client in Clients.Values)
                if (client.IsReady)
                    WebSocket.Sessions.SendTo(binary, client.ID);
        }
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

        public Dictionary<NetworkClientID, NetworkClient> Clients { get; protected set; }

        public Dictionary<NetworkEntityID, NetworkEntity> Entities { get; protected set; }

        public List<NetworkMessage> MessageBuffer { get; protected set; }

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

            while(true)
            {
                if (ActionQueue.Dequeue(out var callback))
                    callback();
                else
                    break;
            }
        }

        void ClientConnected(NetworkClientID id)
        {
            Log.Info($"Room {this.ID}: Client {id} Connected");
        }

        #region Messages
        void ClientMessageCallback(NetworkClientID id, byte[] raw, NetworkMessage message)
        {
            Log.Info($"Room {this.ID}: Client {id} Sent Message With Payload of {message.Type.Name}");

            if(message.Is<RegisterClientRequest>())
            {
                var profile = message.Read<RegisterClientRequest>();

                RegisterClient(id, profile);
            }
            else if (message.Is<ReadyClientRequest>())
            {
                var request = message.Read<ReadyClientRequest>();

                ReadyClient(id);
            }
            else if (Clients.TryGetValue(id, out var client))
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
            }
        }

        void RegisterClient(NetworkClientID id, RegisterClientRequest request) => RegisterClient(id, request.Profile);
        void RegisterClient(NetworkClientID id, NetworkClientProfile profile)
        {
            if (Clients.ContainsKey(id))
            {
                Log.Error($"Client {id} Already Registered, Ignoring Request");
                return;
            }

            var client = new NetworkClient(id, profile);

            Clients.Add(id, client);

            var payload = new ClientConnectedPayload(id, profile);
            Broadcast(payload);

            var info = ReadInternalInfo();
            var response = new RegisterClientResponse(id, info);
            SendTo(id, response);
        }

        void ReadyClient(NetworkClientID id)
        {
            if(Clients.TryGetValue(id, out var client))
            {
                var response = new ReadyClientResponse(client.ID, MessageBuffer);

                client.Ready();

                SendTo(id, response);
            }
            else
            {
                Log.Error($"No Client With ID {id} Found to Mark Ready");
            }
        }

        void InvokeRPC(NetworkClient sender, RpcRequest request)
        {
            var command = RpcCommand.Write(sender.ID, request);

            Broadcast(command);
        }

        void SpawnEntity(NetworkClient owner, SpawnEntityRequest request) => SpawnEntity(owner, request.Resource);
        void SpawnEntity(NetworkClient owner, string resource)
        {
            var id = NetworkEntityID.Generate();

            var command = SpawnEntityCommand.Write(owner.ID, id, resource);
            var response = NetworkMessage.Write(command);

            MessageBuffer.Add(response);

            var entity = new NetworkEntity(id, response);

            owner.RegisterEntity(entity);
            Entities.Add(id, entity);

            Broadcast(response);
        }
        #endregion

        void ClientDisconnected(NetworkClientID id)
        {
            Log.Info($"Room {this.ID}: Client {id} Disconnected");

            if (Clients.TryGetValue(id, out var client))
                RemoveClient(client);
        }

        void RemoveClient(NetworkClient client)
        {
            Clients.Remove(client.ID);

            for (int i = 0; i < client.Entities.Count; i++)
                Entities.Remove(client.Entities[i].ID);

            var payload = new ClientDisconnectPayload(client.ID, client.Profile);

            Broadcast(payload);
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

            ActionQueue = new RoomActionQueue();

            GameServer.WebSocket.AddService<WebSocketService>(Path, InitializeService);

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