using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
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

        public RoomBasicInfo() { }
        public RoomBasicInfo(ushort id, string name, int maxPlayers, int playersCount, AttributesCollection attributes)
        {
            this.id = id;
            this.name = name;
            this.maxPlayers = maxPlayers;
            this.playersCount = playersCount;
            this.attributes = attributes;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(id);
            writer.Write(name);
            writer.Write(maxPlayers);
            writer.Write(playersCount);
            writer.Write(attributes);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out id);
            reader.Read(out name);
            reader.Read(out maxPlayers);
            reader.Read(out playersCount);
            reader.Read(out attributes);
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
        public void Serialize(NetworkWriter writer)
        {

        }
        public void Deserialize(NetworkReader reader)
        {

        }

        public RoomInternalInfo() { }
    }
}