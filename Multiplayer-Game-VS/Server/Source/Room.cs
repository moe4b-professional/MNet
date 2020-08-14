using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Server;

using Game.Fixed;

namespace Game.Server
{
    class Room
    {
        public string ID { get; protected set; }

        public string Name { get; protected set; }

        public int MaxPlayers { get; protected set; }
        public int PlayersCount { get; protected set; }

        public WebSocketServiceHost WebSocket => GameServer.WebSocket.Services[ID];

        public RoomInfo ReadInfo()
        {
            var result = new RoomInfo(ID, Name, MaxPlayers, PlayersCount);

            return result;
        }

        public Room(string id, string name, int maxPlayers)
        {
            this.ID = id;
            this.Name = name;
            this.MaxPlayers = maxPlayers;
        }
    }

    class RoomWebSocketSerivce : WebSocketAPIService
    {
        
    }
}