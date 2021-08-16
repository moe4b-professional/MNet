using System;
using System.Collections.Generic;

using System.Linq;
using System.IO;

using RestRequest = WebSocketSharp.Net.HttpListenerRequest;
using RestResponse = WebSocketSharp.Net.HttpListenerResponse;

using System.Net;
using System.Net.Http;

using System.Threading;
using System.Threading.Tasks;

namespace MNet
{
    static class MasterServer
    {
        public static class Config
        {
            public static LocalConfig Local { get; private set; }

            public static RemoteConfig Remote { get; private set; }

            public static void Configure()
            {
                Local = LocalConfig.Read();

                Remote = Local.GetRemoteConfig();
            }
        }

        public static class Apps
        {
            public static AppConfig[] Array { get; private set; }

            public static Dictionary<AppID, AppConfig> Dictionary { get; private set; }

            internal static void Configure()
            {
                Array = Config.Local.Apps;

                Dictionary = Array.ToDictionary(AppConfig.SelectID);

                Log.Info("Registered Apps:");
                foreach (var app in Array) Log.Info(app);
            }
        }

        public static class Servers
        {
            public static Dictionary<GameServerID, GameServer> Dictionary { get; private set; }

            public static int Count => Dictionary.Count;

            public static class Watchdog
            {
                static List<GameServer> list;

                public const int CheckInterval = 1 * 1000;

                public const int LookupsInterval = 5 * 1000;

                public const int MaxRetries = 2;

                public static async void Run()
                {
                    list = new List<GameServer>();

                    while (true)
                    {
                        if (Count == 0)
                        {
                            await Task.Delay(CheckInterval);
                            continue;
                        }

                        CloneTo(ref list);

                        for (int i = 0; i < list.Count; i++)
                            Lookup(list[i].ID);

                        await Task.Delay(LookupsInterval);
                    }
                }

                public static async void Lookup(GameServerID id)
                {
                    GameServerInfo info;

                    for (int i = 1; i <= MaxRetries; i++)
                    {
                        try
                        {
                            info = await Rest.GET<GameServerInfo>(id.Address, Constants.Server.Game.Rest.Requests.Info);
                        }
                        catch (Exception)
                        {
                            if (i == MaxRetries)
                                break;
                            else
                                continue;
                        }

                        Update(info);
                        return;
                    }

                    Remove(id);
                }
            }

            static RestClientAPI Rest;

            static readonly object SyncLock = new object();

            internal static void Configure()
            {
                Dictionary = new Dictionary<GameServerID, GameServer>();

                RestServerAPI.Router.Register(Constants.Server.Master.Rest.Requests.Server.Register, Register);
                RestServerAPI.Router.Register(Constants.Server.Master.Rest.Requests.Server.Remove, Remove);

                Rest = new RestClientAPI(Constants.Server.Game.Rest.Port, Config.Local.RestScheme);

                Watchdog.Run();
            }

            public static GameServerInfo[] Query()
            {
                lock (SyncLock)
                {
                    var array = Dictionary.ToArray(GameServer.GetInfo);

                    return array;
                }
            }

            public static void CloneTo(ref List<GameServer> target)
            {
                target.Clear();

                lock (SyncLock)
                {
                    target.AddRange(Dictionary.Values);
                }
            }

            #region Register
            static void Register(RestRequest request, RestResponse response)
            {
                if (RestServerAPI.TryRead(request, response, out RegisterGameServerRequest payload) == false) return;

                if (payload.ApiVersion != Constants.ApiVersion)
                {
                    RestServerAPI.Write(response, RestStatusCode.MismatchedApiVersion);
                    return;
                }

                if (payload.Key != ApiKey.Token)
                {
                    RestServerAPI.Write(response, RestStatusCode.InvalidApiKey);
                    return;
                }

                Register(payload.Info);

                var result = new RegisterGameServerResponse(Apps.Array, Config.Remote);
                RestServerAPI.Write(response, result);
            }

            static GameServer Register(GameServerInfo info)
            {
                var server = new GameServer(info);

                lock (SyncLock)
                {
                    Dictionary[info.ID] = server;
                }

                Log.Info($"Registering Server: {server}");

                return server;
            }
            #endregion

            static void Update(GameServerInfo info)
            {
                if (Dictionary.TryGetValue(info.ID, out var server) == false)
                {
                    Log.Warning($"No Server with ID {info.ID} Found to Update");
                    return;
                }

                server.Info = info;
            }

            #region Remove
            static void Remove(RestRequest request, RestResponse response)
            {
                if (RestServerAPI.TryRead(request, response, out RemoveGameServerRequest payload) == false) return;

                Remove(payload.ID);

                var result = new RemoveGameServerResponse(true);
                RestServerAPI.Write(response, result);
            }

            static bool Remove(GameServerID id)
            {
                lock (SyncLock)
                {
                    if (Dictionary.Remove(id))
                    {
                        Log.Info($"Removing Server: {id}");
                        return true;
                    }
                }

                return false;
            }
            #endregion
        }

        public static class REST
        {
            internal static void Configure()
            {
                RestServerAPI.Configure(Constants.Server.Master.Rest.Port);

                RestServerAPI.Router.Register(Constants.Server.Master.Rest.Requests.Scheme, GetScheme);
                RestServerAPI.Router.Register(Constants.Server.Master.Rest.Requests.Info, GetInfo);

                RestServerAPI.Start();
            }

            static void GetScheme(RestRequest request, RestResponse response)
            {
                if (RestServerAPI.TryRead(request, response, out MasterServerSchemeRequest payload) == false) return;

                if (payload.ApiVersion != Constants.ApiVersion)
                {
                    var text = $"Mismatched API Versions [Client: {payload.ApiVersion}, Server: {Constants.ApiVersion}]" +
                        $", Please use the Same Network API Release on the Client and Server";
                    RestServerAPI.Write(response, RestStatusCode.MismatchedApiVersion, text);
                    return;
                }

                if (Apps.Dictionary.TryGetValue(payload.AppID, out var app) == false)
                {
                    RestServerAPI.Write(response, RestStatusCode.InvalidAppID, $"App ID '{payload.AppID}' Not Registered with Server");
                    return;
                }

                if (payload.GameVersion < app.MinimumVersion)
                {
                    var text = $"Version {payload.GameVersion} no Longer Supported, Minimum Supported Version: {app.MinimumVersion}";
                    RestServerAPI.Write(response, RestStatusCode.VersionNotSupported, text);
                    return;
                }

                var info = new MasterServerSchemeResponse(app, Config.Remote);

                RestServerAPI.Write(response, info);
            }

            static void GetInfo(RestRequest request, RestResponse response)
            {
                if (RestServerAPI.TryRead(request, response, out MasterServerInfoRequest payload) == false) return;

                var servers = Servers.Query();

                var info = new MasterServerInfoResponse(servers);

                RestServerAPI.Write(response, info);
            }
        }

        public static class Input
        {
            public static async Task Process()
            {
                while (true)
                    await Poll();
            }

            static async Task Poll()
            {
                var command = ExtraConsole.Read();

                if (await Execute(command) == false)
                    Log.Error($"Unknown Command of '{command}'");
            }

            static async Task<bool> Execute(string command)
            {
                command = command.ToLower();

                switch (command)
                {
                    case "stop":
                        await Stop();
                        break;

                    default:
                        return false;
                }

                return true;
            }
        }

        static async Task Main()
        {
            Console.Title = $"Master Sever | Network API v{Constants.ApiVersion}";

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            ExtraConsoleLog.Bind();

            Log.Info($"Network API Version: {Constants.ApiVersion}");

            ApiKey.Read();

            Config.Configure();
            Apps.Configure();
            Servers.Configure();
            REST.Configure();

            await Input.Process();
        }

        static async Task Stop()
        {
            Log.Info("Closing Server");

            await Task.Delay(400);

            Environment.Exit(0);
        }
    }
}