using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
    [Serializable]
    public class LobbyInfo : INetworkSerializable
    {
        GameServerInfo server;
        public GameServerInfo Server => server;

        RoomBasicInfo[] rooms;
        public RoomBasicInfo[] Rooms { get { return rooms; } }

        public int Size => rooms.Length;

        public RoomBasicInfo this[int index] => rooms[index];

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref server);
            context.Select(ref rooms);
        }

        public LobbyInfo() { }
        public LobbyInfo(GameServerInfo server, RoomBasicInfo[] rooms)
        {
            this.server = server;
            this.rooms = rooms;
        }
    }
}