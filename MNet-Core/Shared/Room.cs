using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
    [Serializable]
    public class RoomBasicInfo : INetworkSerializable
    {
        RoomID id;
        public RoomID ID { get { return id; } }

        string name;
        public string Name { get { return name; } }

        int maxPlayers;
        public int MaxPlayers { get { return maxPlayers; } }

        int playersCount;
        public int PlayersCount { get { return playersCount; } }

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
            context.Select(ref name);
            context.Select(ref maxPlayers);
            context.Select(ref playersCount);
            context.Select(ref attributes);
        }

        public RoomBasicInfo() { }
        public RoomBasicInfo(RoomID id, string name, int maxPlayers, int playersCount, AttributesCollection attributes)
        {
            this.id = id;
            this.name = name;
            this.maxPlayers = maxPlayers;
            this.playersCount = playersCount;
            this.attributes = attributes;
        }

        public override string ToString()
        {
            return "ID: " + ID + Environment.NewLine +
                "Name: " + Name + Environment.NewLine +
                "MaxPlayers: " + MaxPlayers + Environment.NewLine +
                "PlayersCount: " + PlayersCount + Environment.NewLine;
        }
    }

    [Preserve]
    [Serializable]
    public class RoomInternalInfo : INetworkSerializable
    {
        public void Select(INetworkSerializableResolver.Context context)
        {

        }

        public RoomInternalInfo() { }
    }

    public struct RoomID : INetworkSerializable
    {
        uint value;
        public uint Value { get { return value; } }

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref value);
        }

        public RoomID(uint value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(RoomID))
            {
                var target = (RoomID)obj;

                return target.value == this.value;
            }

            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(RoomID a, RoomID b) => a.Equals(b);
        public static bool operator !=(RoomID a, RoomID b) => !a.Equals(b);

        public static RoomID Increment(RoomID id) => new RoomID(id.value + 1);
    }
}