using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
    [Serializable]
    public struct LobbyInfo : INetworkSerializable
    {
        GameServerID server;
        public GameServerID Server => server;

        List<RoomInfo> rooms;
        public List<RoomInfo> Rooms { get { return rooms; } }

        public int Size => rooms == null ? 0 : rooms.Count;

        public RoomInfo this[int index] => rooms[index];

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref server);
            context.Select(ref rooms);
        }

        public LobbyInfo(GameServerID server, List<RoomInfo> rooms)
        {
            this.server = server;
            this.rooms = rooms;
        }
    }
}