using System;
using System.Linq;

using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;
using System.Net;

namespace MNet
{
    static class Lobby
    {
        static class Rooms
        {
            public static Dictionary<AppID, AppCollection> Apps { get; private set; }
            public class AppCollection
            {
                public Dictionary<Version, VersionCollection> Versions { get; protected set; }
                public class VersionCollection
                {
                    public Dictionary<RoomID, Room> Rooms { get; protected set; }

                    public IReadOnlyCollection<RoomID> Keys => Rooms.Keys;
                    public IReadOnlyCollection<Room> Values => Rooms.Values;

                    public void Add(Room room) => Rooms.Add(room.ID, room);

                    public bool Remove(Room room) => Rooms.Remove(room.ID);

                    public VersionCollection()
                    {
                        Rooms = new Dictionary<RoomID, Room>();
                    }
                }

                public void Add(Room room)
                {
                    if (Versions.TryGetValue(room.Version, out var collection) == false)
                    {
                        collection = new VersionCollection();

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
                    Versions = new Dictionary<Version, VersionCollection>();
                }
            }

            public static AutoKeyCollection<RoomID> IDs { get; private set; }

            static readonly object SyncLock = new object();

            public static void Configure()
            {
                Apps = new Dictionary<AppID, AppCollection>();
                IDs = new AutoKeyCollection<RoomID>(RoomID.Min, RoomID.Max, RoomID.Increment, Constants.IdRecycleLifeTime);

                RestServerAPI.Register(Constants.Server.Game.Rest.Requests.Room.Create, Create);
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

            public static void Create(HttpListenerContext context)
            {
                if (RestServerAPI.TryRead(context.Request, context.Response, out CreateRoomRequest payload) == false) return;

                if (AppsAPI.TryGet(payload.AppID, out var app) == false)
                {
                    RestServerAPI.Write(context.Response, RestStatusCode.InvalidAppID);
                    return;
                }

                var room = Create(app, payload.Version, payload.Options);
                var info = room.Info.Get();

                RestServerAPI.Write(context.Response, info);
            }
            public static Room Create(AppConfig app, Version version, RoomOptions options)
            {
                Log.Info($"Creating Room '{options.Name}'");

                var id = Reserve();

                var room = new Room(id, app, version, options.Name, options);

                Add(room);

                room.OnStop += StopCallback;

                room.Start(options);

                return room;
            }

            static void StopCallback(Room room) => Remove(room);

            public static List<RoomInfo> QueryInfo(AppID appID, Version version)
            {
                if (Apps.TryGetValue(appID, out var app) == false)
                    return null;

                IReadOnlyCollection<Room> targets;

                lock (SyncLock) targets = app.Query(version);

                if (targets == null)
                    return null;

                var list = new List<RoomInfo>(targets.Count);

                foreach (var room in targets)
                {
                    if (room.Visible == false) continue;

                    var info = room.Info.Get();

                    list.Add(info);
                }

                return list;
            }
        }

        public static void Configure()
        {
            Rooms.Configure();

            RestServerAPI.Register(Constants.Server.Game.Rest.Requests.Lobby.Info, GetInfo);
        }

        public static void GetInfo(HttpListenerContext context)
        {
            if (RestServerAPI.TryRead(context.Request, context.Response, out GetLobbyInfoRequest payload) == false) return;

            var list = Rooms.QueryInfo(payload.AppID, payload.Version);

            var info = new LobbyInfo(GameServer.Info.ID, list);

            RestServerAPI.Write(context.Response, info);
        }
    }
}