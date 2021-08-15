using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using System.Net.Http;
using System.Threading;

using System.Text;
using System.IO;
using System.Diagnostics;

using RestRequest = WebSocketSharp.Net.HttpListenerRequest;
using RestResponse = WebSocketSharp.Net.HttpListenerResponse;
using System.Threading.Tasks;

namespace MNet
{
    static partial class GameServer
    {
        public static class Config
        {
            public static LocalConfig Local { get; private set; }
            public static RemoteConfig Remote { get; private set; }

            public static void Set(RemoteConfig config) => Remote = config;

            public static void Configure()
            {
                Local = LocalConfig.Read();
            }
        }

        public static class Info
        {
            public static GameServerID ID { get; private set; }
            public static IPAddress Address
            {
                get => ID.Value;
                private set => ID = new GameServerID(value);
            }

            public static GameServerRegion Region => Config.Local.Region;

            public static GameServerInfo Read() => new GameServerInfo(ID, Region, Statistics.Players.Count);

            internal static void Configure()
            {
                ID = new GameServerID(Config.Local.PersonalAddress);

                Log.Info($"Server ID: {ID}");
                Log.Info($"Server Region: {Region}");

                RestServerAPI.Router.Register(Constants.Server.Game.Rest.Requests.Info, Get);
            }

            static void Get(RestRequest request, RestResponse response)
            {
                var info = Read();

                RestServerAPI.Write(response, info);
            }
        }

        static async Task Main()
        {
            Console.Title = $"Game Sever | Network API v{Constants.ApiVersion}";

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            Log.Info($"Network API Version: {Constants.ApiVersion}");

            ApiKey.Read();

            Config.Configure();
            Info.Configure();

            MasterServer.Configure();
            await MasterServer.Register();

            Realtime.Configure();
            RestServerAPI.Configure(Constants.Server.Game.Rest.Port);
            Lobby.Configure();

            Realtime.Start();
            RestServerAPI.Start();

            while (true)
            {
                var key = Console.ReadKey(true);

                Console.WriteLine();

                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        await Exit();
                        break;
                }
            }
        }

        static async Task Exit()
        {
            Log.Info("Closing Server");

            await Task.Delay(500);

            await MasterServer.Remove();

            Realtime.Close();

            Environment.Exit(0);
        }
    }
}