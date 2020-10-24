using System;
using System.Collections.Generic;
using System.Text;

namespace MNet
{
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