using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Server;

using System.Threading;

using Game.Fixed;

namespace Game.Server
{
    class Room
    {
        #region Basic Properties
        public string ID { get; protected set; }

        public string Path => "/" + ID;

        public string Name { get; protected set; }

        public int MaxPlayers { get; protected set; }

        public int PlayersCount { get; protected set; }

        public RoomInfo ReadInfo() => new RoomInfo(ID, Name, MaxPlayers, PlayersCount);
        #endregion

        #region Web Socket
        public WebSocketServiceHost WebSocket => GameServer.WebSocket.Services[Path];

        public RoomWebSocketSerivce CreateWebSocketService()
        {
            var service = new RoomWebSocketSerivce();

            return service;
        }
        #endregion

        #region Schedule
        public const long DefaultTickInterval = 50;

        public Schedule Schedule { get; protected set; }
        #endregion

        void Init()
        {

        }
        
        void Tick()
        {
            //Log.Info("Room Tick @ " + Schedule.DeltaTime.ToString("F3"));
        }

        public Room(string id, string name, int maxPlayers)
        {
            this.ID = id;
            this.Name = name;
            this.MaxPlayers = maxPlayers;

            GameServer.WebSocket.AddService(Path, CreateWebSocketService);

            Schedule = new Schedule(DefaultTickInterval);
            Schedule.OnInit += Init;
            Schedule.OnTick += Tick;
        }
    }

    class RoomWebSocketSerivce : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            base.OnOpen();

            Log.Info($"WebSocket Client {Context.UserEndPoint.Address} Connected To Room, ID: {this.ID}");

            Context.WebSocket.Send($"Welcome to the Room");
        }

        protected override void OnMessage(MessageEventArgs args)
        {
            base.OnMessage(args);

            Log.Info($"WebSocket Client {Context.UserEndPoint.Address}: '{args.Data}'");
        }

        protected override void OnClose(CloseEventArgs args)
        {
            base.OnClose(args);

            Log.Info($"WebSocket Client Disconnected With Code: {args.Code}");
        }

        public RoomWebSocketSerivce()
        {

        }
    }
}