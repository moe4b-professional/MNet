using System;
using System.Collections.Generic;

using System.Linq;
using System.IO;

using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Net;

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
            public static int Count
            {
                get
                {
                    lock (Dictionary)
                        return Dictionary.Count;
                }
            }

            static RestClientAPI Rest;

            internal static void Configure()
            {
                Dictionary = new Dictionary<GameServerID, GameServer>();

                RestServerAPI.Register(Constants.Server.Master.Rest.Requests.Server.Register, Register);
                RestServerAPI.Register(Constants.Server.Master.Rest.Requests.Server.Unregister, Unregister);

                Rest = new RestClientAPI(Constants.Server.Game.Rest.Port, Config.Local.RestScheme);
            }

            #region Register
            static void Register(HttpListenerContext context)
            {
                if (RestServerAPI.TryRead(context.Request, context.Response, out RegisterGameServerRequest payload) == false) return;

                if (payload.ApiVersion != Constants.ApiVersion)
                {
                    RestServerAPI.Write(context.Response, RestStatusCode.MismatchedApiVersion);
                    return;
                }

                if (payload.Key != ApiKey.Token)
                {
                    RestServerAPI.Write(context.Response, RestStatusCode.InvalidApiKey);
                    return;
                }

                Register(payload.Info);

                var result = new RegisterGameServerResponse(Apps.Array, Config.Remote);
                RestServerAPI.Write(context.Response, result);
            }

            static GameServer Register(GameServerInfo info)
            {
                GameServer server;

                lock (Dictionary)
                {
                    if (Dictionary.TryGetValue(info.ID, out server))
                    {
                        Update(server, info);
                        return server;
                    }
                }

                server = new GameServer(info);

                lock (Dictionary)
                {
                    Add(server);
                }

                Log.Info($"Registering Server: {server}");

                return server;
            }
            #endregion

            static void Add(GameServer server)
            {
                Dictionary.Add(server.ID, server);
            }

            static bool Update(GameServerInfo info)
            {
                lock (Dictionary)
                {
                    if (Dictionary.TryGetValue(info.ID, out var server) == false)
                    {
                        Log.Warning($"No Server with ID {info.ID} Found to Update");
                        return false;
                    }

                    Update(server, info);
                    return true;
                }
            }
            static void Update(GameServer server, GameServerInfo info)
            {
                lock (Dictionary)
                {
                    server.Info = info;
                }
            }

            public static GameServerInfo[] Query()
            {
                lock (Dictionary)
                {
                    var array = Dictionary.ToArray(GameServer.GetInfo);

                    return array;
                }
            }

            public static bool Contains(GameServerID id)
            {
                lock (Dictionary)
                {
                    return Dictionary.ContainsKey(id);
                }
            }

            public static void CloneTo(ref List<GameServer> target)
            {
                target.Clear();

                lock (Dictionary)
                {
                    target.AddRange(Dictionary.Values);
                }
            }

            #region Unregister
            static void Unregister(HttpListenerContext context)
            {
                if (RestServerAPI.TryRead(context.Request, context.Response, out RemoveGameServerRequest payload) == false) return;

                Unregister(payload.ID);

                var result = new RemoveGameServerResponse(true);
                RestServerAPI.Write(context.Response, result);
            }

            public static bool Unregister(GameServerID id)
            {
                lock (Dictionary)
                {
                    if (Dictionary.Remove(id))
                    {
                        Log.Info($"Unregistering Server: {id}");
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

                RestServerAPI.Register(Constants.Server.Master.Rest.Requests.Scheme, GetScheme);
                RestServerAPI.Register(Constants.Server.Master.Rest.Requests.Info, GetInfo);

                RestServerAPI.Start();
            }

            static void GetScheme(HttpListenerContext context)
            {
                if (RestServerAPI.TryRead(context.Request, context.Response, out MasterServerSchemeRequest payload) == false) return;

                if (payload.ApiVersion != Constants.ApiVersion)
                {
                    var text = $"Mismatched API Versions [Client: {payload.ApiVersion}, Server: {Constants.ApiVersion}]" +
                        $", Please use the Same Network API Release on the Client and Server";
                    RestServerAPI.Write(context.Response, RestStatusCode.MismatchedApiVersion, text);
                    return;
                }

                if (Apps.Dictionary.TryGetValue(payload.AppID, out var app) == false)
                {
                    RestServerAPI.Write(context.Response, RestStatusCode.InvalidAppID, $"App ID '{payload.AppID}' Not Registered with Server");
                    return;
                }

                if (payload.GameVersion < app.MinimumVersion)
                {
                    var text = $"Version {payload.GameVersion} no Longer Supported, Minimum Supported Version: {app.MinimumVersion}";
                    RestServerAPI.Write(context.Response, RestStatusCode.VersionNotSupported, text);
                    return;
                }

                var servers = Servers.Query();
                var info = new MasterServerInfoResponse(servers);

                var scheme = new MasterServerSchemeResponse(app, Config.Remote, info);

                RestServerAPI.Write(context.Response, scheme);
            }

            static void GetInfo(HttpListenerContext context)
            {
                if (RestServerAPI.TryRead(context.Request, context.Response, out MasterServerInfoRequest payload) == false) return;

                var servers = Servers.Query();

                var info = new MasterServerInfoResponse(servers);

                RestServerAPI.Write(context.Response, info);
            }
        }

        public static class Input
        {
            public static void Process()
            {
                while (true)
                {
                    var command = ExtraConsole.Read();

                    if (Dispatcher.Invoke(command) == false)
                        Log.Error($"Unknown Command of '{command}'");
                }
            }

            public static InputDispatcher Dispatcher { get; private set; }

            static class Servers
            {
                internal static void Configure()
                {
                    Dispatcher.Register("Servers List", List);
                    Dispatcher.Register("Servers Remove", Remove);
                }

                static void List(InputDispatcher.Request request)
                {
                    var list = MasterServer.Servers.Query();

                    if (list.Length == 0)
                    {
                        Log.Info("No Servers Registered");
                        return;
                    }

                    var builder = new StringBuilder();

                    builder.AppendLine($"Server List [{list.Length}]:");

                    for (int i = 0; i < list.Length; i++)
                    {
                        builder.Append(i + 1);
                        builder.Append("- ");

                        builder.Append(list[i]);

                        if (i + 1 > list.Length) builder.AppendLine();
                    }

                    Log.Info(builder);
                }

                static void Remove(InputDispatcher.Request request)
                {
                    var id = request[0].Parse(GameServerID.Parse);

                    MasterServer.Servers.Unregister(id);
                }
            }

            static void Stop(InputDispatcher.Request request) => MasterServer.Stop().Forget();

            static Input()
            {
                Dispatcher = new InputDispatcher();

                Dispatcher.Register("Stop", Stop);

                Servers.Configure();
            }
        }

        static void Main()
        {
            Console.Title = $"Master Sever | Network API v{Constants.ApiVersion}";

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            ExtraConsoleLog.Bind();

            Log.Info($"Network API Version: {Constants.ApiVersion}");

            ApiKey.Read();

            REST.Configure();
            Config.Configure();
            Apps.Configure();
            Servers.Configure();

            Input.Process();
        }

        static async Task Stop()
        {
            Log.Info("Closing Server");

            await Task.Delay(400);

            Environment.Exit(0);
        }
    }
}