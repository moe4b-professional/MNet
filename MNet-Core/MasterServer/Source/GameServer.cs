using System;
using System.Collections.Generic;
using System.Text;

namespace MNet
{
    class GameServer
    {
        public GameServerID ID { get; protected set; }

        public GameServerRegion Region { get; protected set; }

        public GameServerInfo GetInfo() => new GameServerInfo(ID, Region);
        public static GameServerInfo GetInfo(GameServer server) => server.GetInfo();

        public GameServer(GameServerID id, GameServerRegion region)
        {
            this.ID = id;
            this.Region = region;
        }
    }
}
