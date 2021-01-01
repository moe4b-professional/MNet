using System;
using System.Net;
using System.Collections.Generic;

using System.Threading;
using System.Linq;
using System.IO;

using RestRequest = WebSocketSharp.Net.HttpListenerRequest;
using RestResponse = WebSocketSharp.Net.HttpListenerResponse;

namespace MNet
{
    static class MasterServer
    {
        public static Config Config { get; private set; }

        public static RemoteConfig RemoteConfig { get; private set; }

        public static Dictionary<AppID, AppConfig> Apps { get; private set; }

        public static Dictionary<GameServerID, GameServer> Servers { get; private set; }

        static readonly object SyncLock = new object();

        static void Main()
        {
            try
            {
                Procedure();
            }
            catch (Exception)
            {
                throw;
            }

            while (true) Console.ReadLine();
        }

        static void Procedure()
        {
            Console.Title = $"Master Sever | Network API v{Constants.ApiVersion}";

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            Log.Info($"Network API Version: {Constants.ApiVersion}");

            ApiKey.Read();

            Config = Config.Read();

            RemoteConfig = new RemoteConfig(Config.Transport);

            Apps = Config.Apps.ToDictionary(AppConfig.SelectID);

            Log.Info("Registered Apps:");
            foreach (var app in Apps.Values) Log.Info(app);

            RestServerAPI.Configure(Constants.Server.Master.Rest.Port);

            RestServerAPI.Router.Register(Constants.Server.Master.Rest.Requests.Scheme, SendScheme);
            RestServerAPI.Router.Register(Constants.Server.Master.Rest.Requests.Info, SendInfo);
            RestServerAPI.Router.Register(Constants.Server.Master.Rest.Requests.Server.Register, RegisterServer);
            RestServerAPI.Router.Register(Constants.Server.Master.Rest.Requests.Server.Remove, RemoveServer);

            RestServerAPI.Start();
        }

        static void SendScheme(RestRequest request, RestResponse response)
        {
            if (RestServerAPI.TryRead(request, response, out MasterServerSchemeRequest payload) == false) return;

            if (payload.ApiVersion != Constants.ApiVersion)
            {
                var text = $"Mismatched API Versions [Client: {payload.ApiVersion}, Server: {Constants.ApiVersion}]" +
                    $", Please use the Same Network API Release on the Client and Server";
                RestServerAPI.Write(response, RestStatusCode.MismatchedApiVersion, text);
                return;
            }

            if (Apps.TryGetValue(payload.AppID, out var app) == false)
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

            var info = new MasterServerSchemeResponse(app, RemoteConfig);

            RestServerAPI.Write(response, info);
        }

        static void SendInfo(RestRequest request, RestResponse response)
        {
            if (RestServerAPI.TryRead(request, response, out MasterServerInfoRequest payload) == false) return;

            var servers = Query();

            var info = new MasterServerInfoResponse(servers);

            RestServerAPI.Write(response, info);
        }

        static GameServerInfo[] Query()
        {
            lock (SyncLock)
            {
                var list = Servers.ToArray(GameServer.GetInfo);

                return list;
            }
        }

        #region Register Server
        static void RegisterServer(RestRequest request, RestResponse response)
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

            RegisterServer(payload.Info);

            var apps = Apps.Values.ToArray();

            var result = new RegisterGameServerResponse(apps, RemoteConfig);
            RestServerAPI.Write(response, result);
        }

        static GameServer RegisterServer(GameServerInfo info)
        {
            var server = new GameServer(info);

            lock (SyncLock) Servers[info.ID] = server;

            Log.Info($"Registering Server: {server}");

            return server;
        }
        #endregion

        #region Remove Server
        static void RemoveServer(RestRequest request, RestResponse response)
        {
            if (RestServerAPI.TryRead(request, response, out RemoveGameServerRequest payload) == false) return;
            
            RemoveServer(payload.ID);

            var result = new RemoveGameServerResponse(true);
            RestServerAPI.Write(response, result);
        }

        static bool RemoveServer(GameServerID id)
        {
            lock (SyncLock)
            {
                if (Servers.Remove(id))
                {
                    Log.Info($"Removing Server: {id}");
                    return true;
                }
            }

            return false;
        }
        #endregion

        static MasterServer()
        {
            Servers = new Dictionary<GameServerID, GameServer>();
        }
    }
}