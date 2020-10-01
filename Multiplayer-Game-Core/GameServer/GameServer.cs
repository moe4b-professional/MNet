using System;
using System.Net;

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

        public static GameServerInfo GetInfo() => new GameServerInfo(ID, Region);

        public static Config Config { get; private set; }

        public static GameServerRegion Region { get; private set; }

        public static RestAPI Rest { get; private set; }
        public static RealtimeAPI Realtime { get; private set; }

        public static Lobby Lobby { get; private set; }

        static void Main(string[] args)
        {
            Console.Title = "Game Sever";

            Config = Config.Read();
            ApiKey.Read();

            Address = Config.PublicAddress;
            Region = GameServerRegion.Europe;

            MasterServer.Configure(Config.MasterAddress);
            MasterServer.Register(ID, Region, ApiKey.Token);

            Rest = new RestAPI(Constants.GameServer.Rest.Port);
            Rest.Start();

            Realtime = new RealtimeAPI(Config.NetworkTransport);
            Realtime.Start();

            Lobby = new Lobby();
            Lobby.Configure();

            while (true) Console.ReadLine();
        }
    }
}