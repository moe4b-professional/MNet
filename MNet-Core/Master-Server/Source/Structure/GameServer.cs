using System;
using System.Text;
using System.Collections.Generic;

namespace MNet
{
    [Serializable]
    public class GameServer
    {
        GameServerInfo info;
        public GameServerInfo Info
        {
            get => info;
            set => info = value;
        }

        public GameServerID ID => info.ID;
        public GameServerRegion Region => info.Region;

        public override string ToString() => info.ToString();

        public GameServer(GameServerInfo info)
        {
            this.info = info;
        }

        //Static Utility

        public static GameServerInfo GetInfo(GameServer server) => server.Info;
    }
}