using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;

namespace Backend
{
    class Lobby
    {
        public AutoKeyDictionary<RoomID, Room> Rooms { get; protected set; }

        public RoomBasicInfo[] ReadRoomsInfo()
        {
            var results = new RoomBasicInfo[Rooms.Count];

            var index = 0;

            foreach (var room in Rooms.Values)
            {
                var info = room.ReadBasicInfo();

                results[index] = info;

                index += 1;
            }

            return results;
        }

        public RestAPI Rest => GameServer.Rest;

        public void Configure()
        {
            Rest.Router.Register(Constants.GameServer.Rest.Requests.Lobby.Info, GetInfo);
            Rest.Router.Register(Constants.GameServer.Rest.Requests.Room.Create, CreateRoom);
        }

        public void GetInfo(HttpListenerRequest request, HttpListenerResponse response)
        {
            var rooms = ReadRoomsInfo();

            var info = new LobbyInfo(GameServer.GetInfo(), rooms);

            RestAPI.WriteTo(response, info);
        }

        public void CreateRoom(HttpListenerRequest request, HttpListenerResponse response)
        {
            CreateRoomRequest payload;
            try
            {
                payload = RestAPI.Read<CreateRoomRequest>(request);
            }
            catch (Exception)
            {
                RestAPI.WriteTo(response, HttpStatusCode.NotAcceptable, $"Error Reading {nameof(CreateRoomRequest)}");
                return;
            }

            var info = CreateRoom(payload);
            RestAPI.WriteTo(response, info);
        }
        public RoomBasicInfo CreateRoom(CreateRoomRequest request)
        {
            var room = CreateRoom(request.Name, request.Capacity, request.Attributes);

            var info = room.ReadBasicInfo();

            return info;
        }
        public Room CreateRoom(string name, byte capacity, AttributesCollection attributes)
        {
            Log.Info($"Creating Room '{name}'");

            var id = Rooms.Reserve();

            var room = new Room(id, name, capacity, attributes);

            Rooms.Assign(id, room);

            room.Start();

            return room;
        }

        public Lobby()
        {
            Rooms = new AutoKeyDictionary<RoomID, Room>(RoomID.Increment);
        }
    }
}