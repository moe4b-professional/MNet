using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    [NetworkMessagePayload(1)]
    public class LobbyInfo : INetSerializable
    {
        RoomInfo[] rooms;
        public RoomInfo[] Rooms { get { return rooms; } }

        public int Size => rooms.Length;

        public RoomInfo this[int index] => rooms[index];

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(rooms);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out rooms);
        }

        public LobbyInfo() { }
        public LobbyInfo(RoomInfo[] rooms)
        {
            this.rooms = rooms;
        }
    }
}
