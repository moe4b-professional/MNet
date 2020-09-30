using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    public class LobbyInfo : INetworkSerializable
    {
        RoomBasicInfo[] rooms;
        public RoomBasicInfo[] Rooms { get { return rooms; } }

        public int Size => rooms.Length;

        public RoomBasicInfo this[int index] => rooms[index];

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref rooms);
        }

        public LobbyInfo() { }
        public LobbyInfo(RoomBasicInfo[] rooms)
        {
            this.rooms = rooms;
        }
    }
}