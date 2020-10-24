using System;
using System.Text;
using System.Collections.Generic;

namespace MNet
{
    [Serializable]
    public struct GameServer
    {
        GameServerInfo info;
        public GameServerInfo Info => info;

        public GameServerID ID => info.ID;
        public string Name => info.Name;
        public GameServerRegion Region => info.Region;

        public static GameServerInfo GetInfo(GameServer server) => server.Info;

        public override string ToString() => info.ToString();

        public GameServer(GameServerInfo info)
        {
            this.info = info;
        }
    }
}