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
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

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
            RestServerAPI.Start();

            RestServerAPI.Router.Register(Constants.Server.Master.Rest.Requests.Info, GetInfo);
            RestServerAPI.Router.Register(Constants.Server.Master.Rest.Requests.Server.Register, RegisterServer);
            RestServerAPI.Router.Register(Constants.Server.Master.Rest.Requests.Server.Remove, RemoveServer);
        }

        static void GetInfo(RestRequest request, RestResponse response)
        {
            MasterServerInfoRequest payload;
            try
            {
                RestServerAPI.Read(request, out payload);
            }
            catch (Exception)
            {
                RestServerAPI.Write(response, RestStatusCode.InvalidPayload, $"Error Reading Request");
                return;
            }

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

            var list = Query();

            var info = new MasterServerInfoResponse(app, list, RemoteConfig);

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
            RegisterGameServerRequest payload;
            try
            {
                RestServerAPI.Read(request, out payload);
            }
            catch (Exception)
            {
                RestServerAPI.Write(response, RestStatusCode.InvalidPayload, $"Error Reading Request");
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
            RemoveGameServerRequest payload;
            try
            {
                RestServerAPI.Read(request, out payload);
            }
            catch (Exception)
            {
                RestServerAPI.Write(response, RestStatusCode.InvalidPayload, $"Error Reading Request");
                return;
            }

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