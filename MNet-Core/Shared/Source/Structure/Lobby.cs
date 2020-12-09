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

        List<RoomBasicInfo> rooms;
        public List<RoomBasicInfo> Rooms { get { return rooms; } }

        public int Size => rooms.Count;

        public RoomBasicInfo this[int index] => rooms[index];

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref server);
            context.Select(ref rooms);
        }

        public LobbyInfo(GameServerID server, List<RoomBasicInfo> rooms)
        {
            this.server = server;
            this.rooms = rooms;
        }
    }
}