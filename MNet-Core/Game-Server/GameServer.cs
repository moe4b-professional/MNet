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

            public static IPAddress ResolvePersonalAddress()
            {
                if (Local.PersonalAddress != null) return Local.PersonalAddress;

                if (Local.Region == GameServerRegion.Local) return IPAddress.Loopback;

                Log.Info("Retrieving Public IP");
                try
                {
                    return PublicIP.Retrieve();
                }
                catch (Exception)
                {
                    var text = $"Could not Retrieve Public IP for Server, " +
                        $"Please Explicitly Set {nameof(Local.PersonalAddress)} Property in {LocalConfig.FileName} Config File";

                    Log.Error(text);

                    throw;
                }
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
                Address = Config.ResolvePersonalAddress();

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

        static void Main()
        {
            try
            {
                Procedure();
            }
            catch
            {
                throw;
            }

            Sandbox.Run();

            while (true)
            {
                var key = Console.ReadKey();

                Console.WriteLine();

                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        Exit();
                        break;
                }
            }
        }

        static void Procedure()
        {
            Console.Title = $"Game Sever | Network API v{Constants.ApiVersion}";

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            Log.Info($"Network API Version: {Constants.ApiVersion}");

            ApiKey.Read();

            Config.Configure();
            Info.Configure();

            MasterServer.Configure();
            MasterServer.Register();

            Realtime.Configure();
            RestServerAPI.Configure(Constants.Server.Game.Rest.Port);
            Lobby.Configure();

            Realtime.Start();
            RestServerAPI.Start();
        }

        static void Exit()
        {
            Log.Info("Closing Server");

            MasterServer.Remove();

            Realtime.Close();

            Thread.Sleep(2000);

            Environment.Exit(0);
        }
    }

    public static class Sandbox
    {
#pragma warning disable IDE0051
        public static void Run()
        {

        }

        static void Measure(Action action)
        {
            var stopwatch = Stopwatch.StartNew();

            action();

            stopwatch.Stop();

            Log.Info($"{action.Method.Name} Toook {stopwatch.ElapsedMilliseconds.ToString("N")}");
        }
#pragma warning restore IDE0051
    }
}