﻿using System;
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

        public int PlayersCount { get; protected set; }

        public RoomInfo ReadInfo() => new RoomInfo(ID, Name, Capacity, PlayersCount);
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

                void Invoke() => Room.ClientMessage(this.ID, args.RawData, message);
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

        void SendTo(NetworkMessage message, string clientID)
        {
            var binary = NetworkSerializer.Serialize(message);

            WebSocket.Sessions.SendTo(binary, clientID);
        }

        void Broadcast(NetworkMessage message)
        {
            var binary = NetworkSerializer.Serialize(message);

            WebSocket.Sessions.Broadcast(binary);
        }
        #endregion

        #region Schedule
        public Schedule Schedule { get; protected set; }

        public const long DefaultTickInterval = 50;
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

            while(true)
            {
                if (ActionQueue.Dequeue(out var callback))
                    callback();
                else
                    break;
            }
        }

        #region Callbacks
        void ClientConnected(string clientID)
        {
            Log.Info($"Room {this.ID}: Client {clientID} Connected");
        }

        void ClientMessage(string clientID, byte[] raw, NetworkMessage message)
        {
            Log.Info($"Room {this.ID}: Client {clientID} Sent Message With Payload of {message.Type.Name}");

            if(message.Is<ReadyClientRequest>())
            {
                var request = message.Read<ReadyClientRequest>();

                ReadyClient(clientID, request);
            }

            if(message.Is<RpcPayload>())
            {
                var payload = message.Read<RpcPayload>();

                InvokeRPC(clientID, payload, raw);
            }

            if(message.Is<SpawnEntityRequest>())
            {
                var request = message.Read<SpawnEntityRequest>();

                SpawnEntity(clientID, request);
            }
        }

        void ClientDisconnected(string clientID)
        {
            Log.Info($"Room {this.ID}: Client {clientID} Disconnected");
        }
        #endregion

        protected void ReadyClient(string clientID, ReadyClientRequest request)
        {
            var response = new ReadyClientResponse(clientID);

            var message = NetworkMessage.Write(response);

            SendTo(message, clientID);
        }

        protected void InvokeRPC(string clientID, RpcPayload payload, byte[] raw)
        {
            WebSocket.Sessions.Broadcast(raw);
        }

        protected void SpawnEntity(string clientID, SpawnEntityRequest request)
        {
            var id = Guid.NewGuid().ToString("N");

            var command = new SpawnEntityCommand(clientID, request, id);

            var response = NetworkMessage.Write(command);

            Broadcast(response);
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