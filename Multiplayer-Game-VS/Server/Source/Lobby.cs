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
        public Dictionary<ushort, Room> Rooms { get; protected set; }

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

        ushort index;
        public ushort GenerateRoomID()
        {
            var value = index;

            if (index == ushort.MaxValue) index = 0; //Let's just hope that you'll never have more than 65,535 rooms in a single server :)

            index += 1;

            return value;
        }

        public void Configure()
        {
            Rooms = new Dictionary<ushort, Room>();

            GameServer.Rest.Router.Register(RESTRoute);
        }

        public RoomInfo CreateRoom(CreateRoomRequest request)
        {
            var room = CreateRoom(request.Name, request.Capacity);

            var info = room.ReadInfo();

            return info;
        }
        public Room CreateRoom(string name, ushort capacity)
        {
            var id = GenerateRoomID();

            var room = new Room(id, name, capacity);

            Rooms.Add(id, room);

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
    }
}