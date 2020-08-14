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

            GameServer.Rest.Router.Register(GETListRooms);
        }

        public Room CreateRoom(string name)
        {
            var id = Guid.NewGuid().ToString();

            var room = new Room(id, name, 4);

            Rooms.Add(id, room);

            return room;
        }

        public bool GETListRooms(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.RawUrl == Constants.RestAPI.Requests.ListRooms)
            {
                var list = ReadRoomsInfo();

                var message = new ListRoomsMessage(list);

                message.WriteTo(response);

                return true;
            }

            return false;
        }
    }
}