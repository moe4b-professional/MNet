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
    static class Lobby
    {
        public static RoomCollection Rooms { get; private set; }

        static readonly object SyncLock = new object();

        public static void Configure()
        {
            Rooms = new RoomCollection();

            RestAPI.Router.Register(Constants.Server.Game.Rest.Requests.Lobby.Info, GetInfo);
            RestAPI.Router.Register(Constants.Server.Game.Rest.Requests.Room.Create, CreateRoom);
        }

        public static void GetInfo(HttpListenerRequest request, HttpListenerResponse response)
        {
            GetLobbyInfoRequest payload;
            try
            {
                RestAPI.Read(request, out payload);
            }
            catch (Exception)
            {
                RestAPI.Write(response, RestStatusCode.InvalidPayload, $"Error Reading Request");
                return;
            }

            var list = Query(payload.AppID, payload.Version);

            var info = new LobbyInfo(GameServer.ID, list);

            RestAPI.Write(response, info);
        }

        public static List<RoomBasicInfo> Query(AppID appID, Version version)
        {
            var targets = Rooms.Query(appID, version);

            return targets.ToList(Room.GetBasicInfo);
        }

        #region Create Room
        public static void CreateRoom(HttpListenerRequest request, HttpListenerResponse response)
        {
            CreateRoomRequest payload;
            try
            {
                RestAPI.Read(request, out payload);
            }
            catch (Exception)
            {
                RestAPI.Write(response, RestStatusCode.InvalidPayload, $"Error Reading Request");
                return;
            }

            var room = CreateRoom(payload.AppID, payload.Version, payload.Name, payload.Capacity, payload.Attributes);
            var info = room.GetBasicInfo();

            RestAPI.Write(response, info);
        }

        public static Room CreateRoom(AppID appID, Version version, string name, byte capacity, AttributesCollection attributes)
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

        static void RoomStopCallback(Room room) => RemoveRoom(room);

        public static bool RemoveRoom(Room room)
        {
            lock (SyncLock) return Rooms.Remove(room);
        }
    }

    class RoomCollection
    {
        public RoomAppCollection Apps { get; protected set; }

        public AutoKeyCollection<RoomID> IDs { get; protected set; }

        public RoomID Reserve() => IDs.Reserve();

        public void Add(Room room) => Apps.Add(room);

        public bool Remove(Room room)
        {
            if (Apps.Remove(room))
            {
                IDs.Free(room.ID);

                return true;
            }

            return false;
        }

        public IReadOnlyCollection<Room> Query(AppID appID, Version version) => Apps.Query(appID, version);

        public RoomCollection()
        {
            Apps = new RoomAppCollection();

            IDs = new AutoKeyCollection<RoomID>(RoomID.Increment);
        }
    }

    class RoomAppCollection
    {
        public Dictionary<AppID, RoomVersionCollection> Dictionary { get; protected set; }

        public void Add(Room room)
        {
            if (Dictionary.TryGetValue(room.AppID, out var collection) == false)
            {
                collection = new RoomVersionCollection();

                Dictionary.Add(room.AppID, collection);
            }

            collection.Add(room);
        }

        public bool Remove(Room room)
        {
            if (Dictionary.TryGetValue(room.AppID, out var collection) == false)
                return false;

            return collection.Remove(room);
        }

        public IReadOnlyCollection<Room> Query(AppID appID, Version version)
        {
            if (Dictionary.TryGetValue(appID, out var collection) == false)
                return null;

            return collection.Query(version);
        }

        public RoomAppCollection()
        {
            Dictionary = new Dictionary<AppID, RoomVersionCollection>();
        }
    }

    class RoomVersionCollection
    {
        public Dictionary<Version, RoomIDCollection> Dictionary { get; protected set; }

        public void Add(Room room)
        {
            if (Dictionary.TryGetValue(room.Version, out var collection) == false)
            {
                collection = new RoomIDCollection();

                Dictionary.Add(room.Version, collection);
            }

            collection.Add(room);
        }

        public bool Remove(Room room)
        {
            if (Dictionary.TryGetValue(room.Version, out var collection) == false)
                return false;

            return collection.Remove(room);
        }

        public IReadOnlyCollection<Room> Query(Version version)
        {
            if (Dictionary.TryGetValue(version, out var collection) == false)
                return null;

            return collection.Values;
        }

        public RoomVersionCollection()
        {
            Dictionary = new Dictionary<Version, RoomIDCollection>();
        }
    }

    class RoomIDCollection
    {
        public Dictionary<RoomID, Room> Dictionary { get; protected set; }

        public IReadOnlyCollection<RoomID> Keys => Dictionary.Keys;
        public IReadOnlyCollection<Room> Values => Dictionary.Values;

        public void Add(Room room) => Dictionary.Add(room.ID, room);

        public bool Remove(Room room) => Dictionary.Remove(room.ID);

        public RoomIDCollection()
        {
            Dictionary = new Dictionary<RoomID, Room>();
        }
    }
}