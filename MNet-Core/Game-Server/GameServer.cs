using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using System.Threading;

using System.Text;
using System.IO;
using System.Diagnostics;

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

                RestServerAPI.Register(Constants.Server.Game.Rest.Requests.Info, Get);
            }

            static void Get(HttpListenerContext context)
            {
                var info = Read();

                RestServerAPI.Write(context.Response, info);
            }
        }

        public static class Input
        {
            public static InputDispatcher Dispatcher { get; private set; }

            public static void Process()
            {
                while (true)
                {
                    var command = ExtraConsole.Read();

                    if (Dispatcher.Invoke(command) == false)
                        Log.Error($"Unknown Command of '{command}'");
                }
            }

            public static class Stream
            {
                internal static void Configure()
                {
                    Dispatcher.Register("Stream Allocations", Allocations);
                    Dispatcher.Register("Stream Pool Size", PoolSize);
                }

                static void PoolSize(InputDispatcher.Request request)
                {
                    Log.Info($"Network Stream Pool Size: Writer: {NetworkWriter.Pool.Count} | Reader: {NetworkReader.Pool.Count}");
                }

                static void Allocations(InputDispatcher.Request request)
                {
                    Log.Info($"Network Stream Allocated: Writer: {NetworkWriter.Pool.Allocations} | Reader: {NetworkReader.Pool.Allocations}");
                }
            }

            public static class Master
            {
                internal static void Configure()
                {
                    Dispatcher.Register("Master Register", Register);
                    Dispatcher.Register("Master Unregister", Unregister);
                }

                static void Register(InputDispatcher.Request request)
                {
                    Log.Info("Registering Server in Master");

                    MasterServer.Register().Forget();
                }

                static void Unregister(InputDispatcher.Request request)
                {
                    Log.Info("Unregistering Server from Master");

                    MasterServer.Unregister().Forget();
                }
            }

            static void CollectGarbage(InputDispatcher.Request request)
            {
                Log.Info($"Collecting Garbage");
                GC.Collect();
            }

            static void Stop(InputDispatcher.Request request) => GameServer.Stop().Forget();

            static Input()
            {
                Dispatcher = new InputDispatcher();

                Dispatcher.Register("Stop", Stop);
                Dispatcher.Register("Collect Garbage", CollectGarbage);

                Stream.Configure();
                Master.Configure();
            }
        }

        static async Task Main()
        {
            Console.Title = $"Game Sever | Network API v{Constants.ApiVersion}";

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            ExtraConsoleLog.Bind();

            Log.Info($"Network API Version: {Constants.ApiVersion}");

            ApiKey.Read();

            RestServerAPI.Configure(Constants.Server.Game.Rest.Port);

            Config.Configure();
            Info.Configure();

            MasterServer.Configure();
            await MasterServer.Register();

            Realtime.Configure();
            Lobby.Configure();

            Realtime.Start();
            RestServerAPI.Start();

            Input.Process();
        }

        static async Task Stop()
        {
            Log.Info("Closing Server");

            await MasterServer.Unregister();

            Realtime.Close();

            Environment.Exit(0);
        }
    }
}