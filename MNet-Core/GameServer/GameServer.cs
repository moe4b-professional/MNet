using System;
using System.Net;

namespace MNet
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

        public static string[] Versions => Config.Versions;

        public static GameServerRegion Region { get; private set; }

        public static GameServerInfo GetInfo() => new GameServerInfo(ID, Versions, Region);

        public static RestAPI Rest { get; private set; }
        public static RealtimeAPI Realtime { get; private set; }

        public static Lobby Lobby { get; private set; }

        static void Main(string[] args)
        {
            Console.Title = "Game Sever";

            ApiKey.Read();

            Config = Config.Read();

            Log.Info($"Server Versions: {Config.Versions.ToPrettyString()}");

            Address = Config.PublicAddress;
            Region = GameServerRegion.Europe;

            MasterServer.Configure(Config.MasterAddress);
            MasterServer.Register(GetInfo(), ApiKey.Token);

            Rest = new RestAPI(Constants.Server.Game.Rest.Port);
            Rest.Start();

            Realtime = new RealtimeAPI(Config.NetworkTransport);
            Realtime.Start();

            Lobby = new Lobby();
            Lobby.Configure();

            Sandbox();

            while (true) Console.ReadLine();
        }

        static void Sandbox()
        {
            return;

            var tuple = NetTuple.Create("Hello World", 4, DateTime.Now, Guid.NewGuid());
            var type = tuple.GetType();
            var binary = NetworkSerializer.Serialize(tuple);

            var payload = NetworkSerializer.Deserialize(binary, type) as NetTuple;
            foreach (var item in payload) Log.Info(item);
        }
    }
}