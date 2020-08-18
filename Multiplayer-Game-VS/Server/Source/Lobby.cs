using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Game.Shared;

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

            GameServer.Rest.Router.Register(RESTRoute);
        }

        public CreateRoomResponsePayload CreateRoom(CreateRoomRequestPayload request)
        {
            var room = CreateRoom(request.Name, request.Capacity);

            var info = room.ReadInfo();

            var response = new CreateRoomResponsePayload(info);

            return response;
        }
        public Room CreateRoom(string name, short capacity)
        {
            var id = Guid.NewGuid().ToString();

            var room = new Room(id, name, capacity);

            Rooms.Add(id, room);

            room.Start();

            return room;
        }

        public bool RESTRoute(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.RawUrl == Constants.RestAPI.Requests.ListRooms)
            {
                var list = ReadRoomsInfo();

                var message = new ListRoomsPayload(list).ToMessage();

                message.WriteTo(response);

                return true;
            }

            if(request.RawUrl == Constants.RestAPI.Requests.CreateRoom)
            {
                CreateRoomRequestPayload payload;

                try
                {
                    var message = NetworkMessage.Read(request);

                    payload = message.Read<CreateRoomRequestPayload>();
                }
                catch (Exception e)
                {
                    Log.Error($"Error on {Constants.RestAPI.Requests.CreateRoom},\nmessage: {e}");
                    return false;
                }

                {
                    var message = CreateRoom(payload).ToMessage();

                    message.WriteTo(response);

                    return true;
                }
            }

            if(request.RawUrl == "/PlayerInfo")
            {
                var dictionary = new Dictionary<string, string>();

                dictionary.Add("Name", "Moe4B");

                dictionary.Add("Level", "4");

                var message = new PlayerInfoPayload(dictionary).ToMessage();

                message.WriteTo(response);

                return true;
            }

            return false;
        }
    }
}