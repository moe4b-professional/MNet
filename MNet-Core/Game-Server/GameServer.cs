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

        public static string Name => Config.Name;

        public static GameServerRegion Region => Config.Region;

        public static GameServerInfo GetInfo() => new GameServerInfo(ID, Name, Region);

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

            Log.Info($"Server ID: {ID}");
            Log.Info($"Server Name: {Name}");
            Log.Info($"Server Region: {Region}");
            Log.Info($"Tick Delay: {Config.TickDelay}");

            MasterServer.Configure(Config.MasterAddress);

            if (RegisterOnMaster() == false) return;

            RestAPI.Configure(Constants.Server.Game.Rest.Port);
            RestAPI.Start();

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

            return true;
        }
    }

    public static class Sandbox
    {
#pragma warning disable IDE0051
        public static void Run()
        {

        }

        static void Serialize()
        {
            var data = new Data()
            {
                number = 42,
                text = "Hello World",
                date = DateTime.Now,
                guid = Guid.NewGuid(),
            };

            var binary = NetworkSerializer.Serialize(data);

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < 10_000; i++)
            {
                //NetworkSerializer.Deserialize<Data>(binary);
                NetworkSerializer.Serialize(data);
            }

            stopwatch.Stop();

            Log.Info($"Elapsed: {stopwatch.ElapsedMilliseconds}");
        }

        struct Data : INetworkSerializable
        {
            public int number;
            public string text;
            public DateTime? date;
            public Guid? guid;

            public void Select(INetworkSerializableResolver.Context context)
            {
                context.Select(ref number);
                context.Select(ref text);
                context.Select(ref date);
                context.Select(ref guid);
            }
        }
#pragma warning restore IDE0051
    }
}