using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Backend.Shared;

using WebSocketSharp;
using WebSocketSharp.Net;

namespace Backend.Server
{
    class Lobby
    {
        public IDCollection<Room> Rooms { get; protected set; }

        public RoomBasicInfo[] ReadRoomsInfo()
        {
            var results = new RoomBasicInfo[Rooms.Count];

            var index = 0;

            foreach (var room in Rooms.Collection)
            {
                var info = room.ReadBasicInfo();

                results[index] = info;

                index += 1;
            }

            return results;
        }

        public void Configure()
        {
            GameServer.Rest.Router.Register(RESTRoute);
        }

        public RoomBasicInfo CreateRoom(CreateRoomRequest request)
        {
            var room = CreateRoom(request.Name, request.Capacity, request.Attributes);

            var info = room.ReadBasicInfo();

            return info;
        }
        public Room CreateRoom(string name, ushort capacity, AttributesCollection attributes)
        {
            var code = Rooms.Reserve();

            var room = new Room(code, name, capacity, attributes);

            Rooms.Assign(room, code);

            room.Start();

            return room;
        }

        public bool RESTRoute(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.RawUrl == Constants.RestAPI.Requests.Lobby.Info)
            {
                var rooms = ReadRoomsInfo();

                var info = new LobbyInfo(rooms);

                var message = NetworkMessage.Write(info);

                message.WriteTo(response);

                return true;
            }

            if(request.RawUrl == Constants.RestAPI.Requests.Room.Create)
            {
                CreateRoomRequest payload;

                try
                {
                    var message = NetworkMessage.Read(request);

                    payload = message.Read<CreateRoomRequest>();
                }
                catch (Exception)
                {
                    RestAPI.WriteTo(response, HttpStatusCode.NotAcceptable, $"Error Reading {nameof(CreateRoomRequest)}");

                    return true;
                }

                {
                    var info = CreateRoom(payload);

                    var message = NetworkMessage.Write(info);

                    message.WriteTo(response);

                    return true;
                }
            }

            return false;
        }

        public Lobby()
        {
            Rooms = new IDCollection<Room>();
        }
    }
}