using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.IO;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;

namespace Backend
{
    static class GameServer
    {
        public static GameServerID ID { get; private set; }
        public static IPAddress Address
        {
            get => ID.Value;
            private set => ID = new GameServerID(value);
        }

        public static Config Config { get; private set; }

        public static GameServerRegion Region { get; private set; }

        public static RestAPI Rest { get; private set; }
        public static RealtimeAPI Realtime { get; private set; }

        public static Lobby Lobby { get; private set; }

        static void Main(string[] args)
        {
            Config = Config.Read();
            ApiKey.Read();

            Address = Config.PublicAddress;
            Region = GameServerRegion.EU;

            MasterServer.Configure(Config.MasterAddress);
            MasterServer.Register(ID, Region, ApiKey.Token);

            Rest = new RestAPI(IPAddress.Any, Constants.GameServer.Rest.Port);
            Rest.Start();

            Realtime = new RealtimeAPI(IPAddress.Any);
            Realtime.Start();

            Lobby = new Lobby();
            Lobby.Configure();

            while (true) Console.ReadLine();
        }
    }
}