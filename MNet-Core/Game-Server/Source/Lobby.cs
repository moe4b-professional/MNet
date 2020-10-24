using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;

namespace MNet
{
    class Lobby
    {
        public RoomCollection Rooms { get; protected set; }

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

            var list = Query(payload.AppID, payload.Version);

            var info = new LobbyInfo(GameServer.ID, list);

            RestAPI.Write(response, info);
        }

        public List<RoomBasicInfo> Query(AppID appID, Version version)
        {
            var targets = Rooms.Query(appID, version);

            return targets.ToList(Room.GetBasicInfo);
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

            var room = CreateRoom(payload.AppID, payload.Version, payload.Name, payload.Capacity, payload.Attributes);
            var info = room.GetBasicInfo();

            RestAPI.Write(response, info);
        }

        public Room CreateRoom(AppID appID, Version version, string name, byte capacity, AttributesCollection attributes)
        {
            Log.Info($"Creating Room '{name}'");

            Room room;

            lock (SyncLock)
            {
                var id = Rooms.Reserve();

                room = new Room(id, appID, version, name, capacity, attributes);

                Rooms.Add(room);
            }

            room.OnStop += RoomStopCallback;

            room.Start();

            return room;
        }
        #endregion

        void RoomStopCallback(Room room) => RemoveRoom(room);

        public bool RemoveRoom(Room room)
        {
            lock (SyncLock) return Rooms.Remove(room);
        }

        public Lobby()
        {
            Rooms = new RoomCollection();
        }
    }
}