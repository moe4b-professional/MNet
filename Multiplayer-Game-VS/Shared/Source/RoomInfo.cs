using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    [NetworkMessagePayload(2)]
    public class RoomBasicInfo : INetworkMessagePayload, INetSerializable
    {
        ushort id;
        public ushort ID { get { return id; } }

        string name;
        public string Name { get { return name; } }

        int maxPlayers;
        public int MaxPlayers { get { return maxPlayers; } }

        int playersCount;
        public int PlayersCount { get { return playersCount; } }

        public RoomBasicInfo() { }
        public RoomBasicInfo(ushort id, string name, int maxPlayers, int playersCount)
        {
            this.id = id;
            this.name = name;
            this.maxPlayers = maxPlayers;
            this.playersCount = playersCount;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(id);
            writer.Write(name);
            writer.Write(maxPlayers);
            writer.Write(playersCount);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out id);
            reader.Read(out name);
            reader.Read(out maxPlayers);
            reader.Read(out playersCount);
        }

        public override string ToString()
        {
            return "ID: " + ID + Environment.NewLine +
                "Name: " + Name + Environment.NewLine +
                "MaxPlayers: " + MaxPlayers + Environment.NewLine +
                "PlayersCount: " + PlayersCount + Environment.NewLine;
        }
    }

    [NetworkMessagePayload(13)]
    public class RoomInternalInfo : INetworkMessagePayload, INetSerializable
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