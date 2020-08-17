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

        public RoomInfo[] ReadRoomsInfo()
        {
            var results = new RoomInfo[Rooms.Count];

            var index = 0;
            foreach (var room in Rooms.Values)
            {
                var info = room.ReadInfo();

                results[index] = info;

                index++;
            }

            return results;
        }

        public void Configure()
        {
            Rooms = new Dictionary<string, Room>();

            GameServer.Rest.Router.Register(RestRoute);
        }

        public Room CreateRoom(string name)
        {
            var id = Guid.NewGuid().ToString();

            var room = new Room(id, name, 4);

            Rooms.Add(id, room);

            room.Start();

            return room;
        }

        public bool RestRoute(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.RawUrl == Constants.RestAPI.Requests.ListRooms)
            {
                var list = ReadRoomsInfo();

                var payload = new ListRoomsPayload(list);

                var message = NetworkMessage.Write(payload);

                message.WriteTo(response);

                return true;
            }

            if(request.RawUrl == "/PlayerInfo")
            {
                var dictionary = new Dictionary<string, string>();

                dictionary.Add("Name", "Moe4B");

                dictionary.Add("Level", "4");

                var payload = new PlayerInfoPayload(dictionary);

                var message = NetworkMessage.Write(payload);

                message.WriteTo(response);

                return true;
            }

            return false;
        }
    }
}