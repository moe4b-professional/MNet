using System;

using System.Collections.Generic;

using Newtonsoft.Json;

using RestRequest = WebSocketSharp.Net.HttpListenerRequest;
using RestResponse = WebSocketSharp.Net.HttpListenerResponse;

namespace MNet
{
    static class Lobby
    {
        static class Rooms
        {
            public static Dictionary<AppID, AppCollection> Apps { get; private set; }
            public class AppCollection
            {
                public Dictionary<Version, Collection> Versions { get; protected set; }
                public class Collection
                {
                    public Dictionary<RoomID, Room> Dictionary { get; protected set; }

                    public IReadOnlyCollection<RoomID> Keys => Dictionary.Keys;
                    public IReadOnlyCollection<Room> Values => Dictionary.Values;

                    public void Add(Room room) => Dictionary.Add(room.ID, room);

                    public bool Remove(Room room) => Dictionary.Remove(room.ID);

                    public Collection()
                    {
                        Dictionary = new Dictionary<RoomID, Room>();
                    }
                }

                public void Add(Room room)
                {
                    if (Versions.TryGetValue(room.Version, out var collection) == false)
                    {
                        collection = new Collection();

                        Versions.Add(room.Version, collection);
                    }

                    collection.Add(room);
                }

                public bool Remove(Room room)
                {
                    if (Versions.TryGetValue(room.Version, out var collection) == false)
                        return false;

                    return collection.Remove(room);
                }

                public IReadOnlyCollection<Room> Query(Version version)
                {
                    if (Versions.TryGetValue(version, out var collection) == false)
                        return null;

                    return collection.Values;
                }

                public AppCollection()
                {
                    Versions = new Dictionary<Version, Collection>();
                }
            }

            public static AutoKeyCollection<RoomID> IDs { get; private set; }

            static readonly object SyncLock = new object();

            public static void Configure()
            {
                Apps = new Dictionary<AppID, AppCollection>();
                IDs = new AutoKeyCollection<RoomID>(RoomID.Increment, new RoomID(1));

                RestServerAPI.Router.Register(Constants.Server.Game.Rest.Requests.Room.Create, Create);
            }

            public static RoomID Reserve()
            {
                lock (SyncLock) return IDs.Reserve();
            }

            public static void Add(Room room)
            {
                lock (SyncLock)
                {
                    if (Apps.TryGetValue(room.App.ID, out var collection) == false)
                    {
                        collection = new AppCollection();

                        Apps.Add(room.App.ID, collection);
                    }

                    collection.Add(room);
                }
            }
            public static bool Remove(Room room)
            {
                lock (SyncLock)
                {
                    if (Apps.TryGetValue(room.App.ID, out var collection) == false)
                        return false;

                    IDs.Free(room.ID);

                    return collection.Remove(room);
                }
            }

            public static void Create(RestRequest request, RestResponse response)
            {
                if (RestServerAPI.TryRead(request, response, out CreateRoomRequest payload) == false) return;

                if (AppsAPI.TryGet(payload.AppID, out var app) == false)
                {
                    RestServerAPI.Write(response, RestStatusCode.InvalidAppID);
                    return;
                }

                var room = Create(app, payload.Version, payload.Name, payload.Capacity, payload.Attributes);
                var info = room.GetBasicInfo();

                RestServerAPI.Write(response, info);
            }
            public static Room Create(AppConfig app, Version version, string name, byte capacity, AttributesCollection attributes)
            {
                Log.Info($"Creating Room '{name}'");

                var id = Reserve();

                var room = new Room(id, app, version, name, capacity, attributes);

                Add(room);

                room.OnStop += StopCallback;

                room.Start();

                return room;
            }

            static void StopCallback(Room room) => Remove(room);

            public static List<RoomBasicInfo> QueryInfo(AppID appID, Version version)
            {
                if (Apps.TryGetValue(appID, out var app) == false)
                    return null;

                IReadOnlyCollection<Room> target;

                lock (SyncLock) target = app.Query(version);

                return target.ToList(Room.GetBasicInfo);
            }
        }

        public static void Configure()
        {
            Rooms.Configure();

            RestServerAPI.Router.Register(Constants.Server.Game.Rest.Requests.Lobby.Info, GetInfo);
        }

        public static void GetInfo(RestRequest request, RestResponse response)
        {
            if (RestServerAPI.TryRead(request, response, out GetLobbyInfoRequest payload) == false) return;

            var list = Rooms.QueryInfo(payload.AppID, payload.Version);

            var info = new LobbyInfo(GameServer.ID, list);

            RestServerAPI.Write(response, info);
        }
    }
}