using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fixed
{
    public class RoomInfo : INetSerializable
    {
        public string ID { get; protected set; }

        public string Name { get; protected set; }

        public int MaxPlayers { get; protected set; }

        public int PlayersCount { get; protected set; }

        public RoomInfo()
        {

        }
        public RoomInfo(string id, string name, int maxPlayers, int playersCount)
        {
            this.ID = id;

            this.Name = name;

            this.MaxPlayers = maxPlayers;

            this.PlayersCount = playersCount;
        }

        public void Deserialize(NetworkReader reader)
        {
            ID = reader.ReadString();
            Name = reader.ReadString();
            MaxPlayers = reader.ReadInt();
            PlayersCount = reader.ReadInt();
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteString(ID);
            writer.WriteString(Name);
            writer.WriteInt(MaxPlayers);
            writer.WriteInt(PlayersCount);
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