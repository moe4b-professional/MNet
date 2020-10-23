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

        public RestAPI Rest => GameServer.Rest;

        readonly object SyncLock = new object();

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
                RestAPI.Write(response, HttpStatusCode.NotAcceptable, $"Error Reading {nameof(GetLobbyInfoRequest)}");
                return;
            }

            var list = Query(payload.Version);

            var info = new LobbyInfo(GameServer.ID, list);

            RestAPI.Write(response, info);
        }

        public List<RoomBasicInfo> Query(Version version)
        {
            var list = new List<RoomBasicInfo>(Rooms.Count);

            lock (SyncLock)
            {
                foreach (var room in Rooms.Values)
                {
                    if (room.Version != version) continue;

                    var info = room.ReadBasicInfo();

                    list.Add(info);
                }
            }

            return list;
        }

        #region Create Room
        public void CreateRoom(HttpListenerRequest request, HttpListenerResponse response)
        {
            CreateRoomRequest payload;
            try
            {
                RestAPI.Read(request, out payload);
            }
            catch (Exception)
            {
                RestAPI.Write(response, HttpStatusCode.NotAcceptable, $"Error Reading {nameof(CreateRoomRequest)}");
                return;
            }

            var room = CreateRoom(payload.Name, payload.Version, payload.Capacity, payload.Attributes);
            var info = room.ReadBasicInfo();

            RestAPI.Write(response, info);
        }

        public Room CreateRoom(string name, Version version, byte capacity, AttributesCollection attributes)
        {
            Log.Info($"Creating Room '{name}'");

            Room room;

            lock (SyncLock)
            {
                var id = Rooms.Reserve();

                room = new Room(id, name, version, capacity, attributes);

                Rooms.Assign(id, room);
            }

            room.OnStop += RoomStopCallback;

            room.Start();

            return room;
        }
        #endregion

        void RoomStopCallback(Room room) => RemoveRoom(room.ID);

        public bool RemoveRoom(RoomID id)
        {
            lock (SyncLock) return Rooms.Remove(id);
        }

        public Lobby()
        {
            Rooms = new AutoKeyDictionary<RoomID, Room>(RoomID.Increment);
        }
    }
}