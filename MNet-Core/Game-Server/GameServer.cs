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

namespace MNet
{
    static class GameServer
    {
        public static Config Config { get; private set; }

        public static GameServerID ID { get; private set; }
        public static IPAddress Address
        {
            get => ID.Value;
            private set => ID = new GameServerID(value);
        }

        public static GameServerRegion Region => Config.Region;

        public static GameServerInfo GetInfo() => new GameServerInfo(ID, Region);

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

            while (true) Console.ReadLine();
        }

        static void Procedure()
        {
            Console.Title = $"Game Sever | Network API v{Constants.ApiVersion}";

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            Log.Info($"Network API Version: {Constants.ApiVersion}");

            ApiKey.Read();

            Config = Config.Read();

            ResolveAddress();

            MasterServer.Configure(Config.MasterAddress);

            if (RegisterOnMaster() == false) return;

            Log.Info($"Server ID: {ID}");
            Log.Info($"Server Region: {Region}");

            RestServerAPI.Configure(Constants.Server.Game.Rest.Port);
            RestServerAPI.Start();

            RealtimeAPI.Configure(Config.Remote.Transport);
            RealtimeAPI.Start();

            Lobby.Configure();
        }

        static void ResolveAddress()
        {
            if (Config.PublicAddress != null)
            {
                Address = Config.PublicAddress;
                return;
            }

            if (Config.Region == GameServerRegion.Local)
            {
                Address = IPAddress.Parse("127.0.0.1");
                return;
            }

            Log.Info("Retrieving Public IP");
            try
            {
                Address = PublicIP.Retrieve();
            }
            catch (Exception ex)
            {
                var text = $"Could not Retrieve Public IP for Server, " +
                    $"Please Explicitly Set {nameof(Config.PublicAddress)} Property in {Config.FileName} Config File";

                Log.Error(text);

                Log.Error($"Public IP Retrieval Exception: {ex}");
            }
        }

        static bool RegisterOnMaster()
        {
            if (MasterServer.Register(GetInfo(), ApiKey.Token, out var response) == false)
            {
                Log.Error("Server Registeration Failed");
                return false;
            }

            Log.Info("Server Registeration Success");

            Config.Set(response.RemoteConfig);

            AppsAPI.Set(response.Apps);

            return true;
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