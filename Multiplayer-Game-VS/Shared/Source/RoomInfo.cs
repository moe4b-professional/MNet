using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    public class RoomBasicInfo : INetworkSerializable
    {
        ushort id;
        public ushort ID { get { return id; } }

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
        public RoomBasicInfo(ushort id, string name, int maxPlayers, int playersCount, AttributesCollection attributes)
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

    public class RoomInternalInfo : INetworkSerializable
    {
        public void Select(INetworkSerializableResolver.Context context)
        {

        }

        public RoomInternalInfo() { }
    }
}