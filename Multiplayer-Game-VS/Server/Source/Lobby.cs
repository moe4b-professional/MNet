using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Game.Fixed;

using WebSocketSharp;
using WebSocketSharp.Net;

namespace Game.Server
{
    class Lobby
    {
        public Dictionary<string, Room> Rooms { get; protected set; }

        public LobbyRestAPI Rest { get; protected set; }

        public IList<RoomInfo> ReadRoomsInfo()
        {
            var results = new List<RoomInfo>(Rooms.Count);

            foreach (var room in Rooms.Values)
            {
                var info = room.ReadInfo();

                results.Add(info);
            }

            return results;
        }

        public void Configure()
        {
            Rooms = new Dictionary<string, Room>();

            Rest = new LobbyRestAPI(this);
        }

        public Room CreateRoom(string name)
        {
            var id = Guid.NewGuid().ToString();

            var room = new Room(id, name, 4);

            Rooms.Add(id, room);

            return room;
        }
    }

    class LobbyRestAPI
    {
        Lobby lobby;

        public bool ProcessListRooms(HttpListenerRequest request, HttpListenerResponse response)
        {
            if(request.RawUrl == Constants.RestAPI.Requests.ListRooms)
            {
                response.StatusCode = (int)HttpStatusCode.OK;

                var rooms = lobby.ReadRoomsInfo();

                var message = new ListRoomsMessage(rooms);

                var data = NetworkSerializer.Serialize(message);

                response.WriteContent(data);

                response.Close();

                return true;
            }

            return false;
        }

        public LobbyRestAPI(Lobby lobby)
        {
            this.lobby = lobby;

            GameServer.Rest.Router.Register(ProcessListRooms);
        }
    }
}