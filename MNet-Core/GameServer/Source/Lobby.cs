using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;

namespace MNet
{
    class Lobby
    {
        public AutoKeyDictionary<RoomID, Room> Rooms { get; protected set; }

        public RoomBasicInfo[] ReadRoomsInfo() => Rooms.Dictionary.ToArray(Room.ReadBasicInfo);

        public RestAPI Rest => GameServer.Rest;

        public void Configure()
        {
            Rest.Router.Register(Constants.Server.Game.Rest.Requests.Lobby.Info, GetInfo);
            Rest.Router.Register(Constants.Server.Game.Rest.Requests.Room.Create, CreateRoom);
        }

        public void GetInfo(HttpListenerRequest request, HttpListenerResponse response)
        {
            GetLobbyInfoRequest payload;
            try
            {
                RestAPI.Read(request, out payload);
            }
            catch (Exception)
            {
                RestAPI.WriteTo(response, HttpStatusCode.NotAcceptable, $"Error Reading {nameof(GetLobbyInfoRequest)}");
                return;
            }

            var list = new List<RoomBasicInfo>(Rooms.Count);

            foreach (var room in Rooms.Values)
            {
                if (room.Version != payload.Version) continue;

                var instance = room.ReadBasicInfo();

                list.Add(instance);
            }

            var info = new LobbyInfo(GameServer.GetInfo(), list);

            RestAPI.WriteTo(response, info);
        }

        public void CreateRoom(HttpListenerRequest request, HttpListenerResponse response)
        {
            CreateRoomRequest payload;
            try
            {
                RestAPI.Read(request, out payload);
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
            var room = CreateRoom(request.Name, request.Version, request.Capacity, request.Attributes);

            var info = room.ReadBasicInfo();

            return info;
        }
        public Room CreateRoom(string name, Version version, byte capacity, AttributesCollection attributes)
        {
            Log.Info($"Creating Room '{name}'");

            var id = Rooms.Reserve();

            var room = new Room(id, name, version, capacity, attributes);

            Rooms.Assign(id, room);

            room.OnStop += StopRoomCallback;

            room.Start();

            return room;
        }

        void StopRoomCallback(Room room)
        {
            Rooms.Remove(room.ID);
        }

        public Lobby()
        {
            Rooms = new AutoKeyDictionary<RoomID, Room>(RoomID.Increment);
        }
    }
}