using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fixed
{
    [Serializable]
    public struct RoomInfo
    {
        public string ID { get; private set; }

        public string Name { get; private set; }

        public int MaxPlayers { get; private set; }
        public int PlayersCount { get; private set; }

        public RoomInfo(string id, string name, int maxPlayers, int playersCount)
        {
            this.ID = id;

            this.Name = name;

            this.MaxPlayers = maxPlayers;

            this.PlayersCount = playersCount;
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