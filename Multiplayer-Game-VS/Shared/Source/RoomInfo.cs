using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    public class RoomInfo : INetSerializable
    {
        protected ushort id;
        public ushort ID { get { return id; } }

        protected string name;
        public string Name { get { return name; } }

        protected int maxPlayers;
        public int MaxPlayers { get { return maxPlayers; } }

        protected int playersCount;
        public int PlayersCount { get { return playersCount; } }

        public RoomInfo() { }
        public RoomInfo(ushort id, string name, int maxPlayers, int playersCount)
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
}