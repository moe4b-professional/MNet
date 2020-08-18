using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Server;

using System.Threading;

using Game.Shared;

namespace Game.Server
{
    class Room
    {
        #region Basic Properties
        public string ID { get; protected set; }

        public string Path => "/" + ID;

        public string Name { get; protected set; }

        public short Capacity { get; protected set; }

        public int PlayersCount { get; protected set; }

        public RoomInfo ReadInfo() => new RoomInfo(ID, Name, Capacity, PlayersCount);
        #endregion

        #region Web Socket
        public WebSocketServiceHost WebSocket => GameServer.WebSocket.Services[Path];

        public class WebSocketService : WebSocketBehavior
        {
            protected override void OnOpen()
            {
                base.OnOpen();

                Log.Info($"{nameof(Room)}: {this.ID} Connected");

                Context.WebSocket.Send($"Welcome to the Room");
            }

            protected override void OnMessage(MessageEventArgs args)
            {
                base.OnMessage(args);

                Log.Info($"{nameof(Room)}: {this.ID} says '{args.Data}'");
            }

            protected override void OnClose(CloseEventArgs args)
            {
                base.OnClose(args);

                Log.Info($"{nameof(Room)}: {this.ID} Disconnected, code {args.Code}");
            }

            public WebSocketService()
            {

            }

            public static void Create(WebSocketService service)
            {

            }
        }
        #endregion

        #region Schedule
        public Schedule Schedule { get; protected set; }

        public const long DefaultTickInterval = 50;
        #endregion

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
        }

        public void Stop()
        {
            Log.Info($"Stopping Room: {ID}");

            Schedule.Stop();

            GameServer.WebSocket.RemoveService(Path);
        }

        public Room(string id, string name, short capacity)
        {
            this.ID = id;
            this.Name = name;
            this.Capacity = capacity;

            GameServer.WebSocket.AddService<WebSocketService>(Path, WebSocketService.Create);

            Schedule = new Schedule(DefaultTickInterval, Init, Tick);
        }
    }
}