using System;
using System.Net;
using System.Collections.Generic;

using SharpHttpCode = WebSocketSharp.Net.HttpStatusCode;
using SharpHttpRequest = WebSocketSharp.Net.HttpListenerRequest;
using SharpHttpResponse = WebSocketSharp.Net.HttpListenerResponse;

using System.Threading;
using System.Linq;
using System.Diagnostics;

namespace MNet
{
    static class MasterServer
    {
        public static Config Config { get; private set; }

        public static Version MinimumVersion => Config.MinimumVersion;

        public static RestAPI Rest { get; private set; }

        public static Dictionary<GameServerID, GameServer> Servers { get; private set; }

        static object SyncLock = new object();

        static void Main(string[] args)
        {
            Console.Title = $"Master Sever | Network API v{Constants.ApiVersion}";

            Log.Info($"Network API Version: {Constants.ApiVersion}");

            ApiKey.Read();

            Config = Config.Read();

            Log.Info($"Minimum Game Version: {MinimumVersion}");

            Rest = new RestAPI(Constants.Server.Master.Rest.Port);
            Rest.Start();

            Rest.Router.Register(Constants.Server.Master.Rest.Requests.Info, GetInfo);
            Rest.Router.Register(Constants.Server.Master.Rest.Requests.Server.Register, RegisterServer);
            Rest.Router.Register(Constants.Server.Master.Rest.Requests.Server.Remove, RemoveServer);

            while (true) Console.ReadLine();
        }

        static void GetInfo(SharpHttpRequest request, SharpHttpResponse response)
        {
            MasterServerInfoRequest payload;
            try
            {
                RestAPI.Read(request, out payload);
            }
            catch (Exception)
            {
                RestAPI.Write(response, SharpHttpCode.NotAcceptable, $"Error Reading Info Request");
                return;
            }

            if (payload.ApiVersion != Constants.ApiVersion)
            {
                var text = $"Mismatched API Versions [Client: {payload.ApiVersion}, Server: {Constants.ApiVersion}]" +
                    $", Please use the Same Network API Release on the Client and Server";
                RestAPI.Write(response, SharpHttpCode.Gone, text);
                return;
            }

            if (payload.GameVersion < MinimumVersion)
            {
                var text = $"Version {payload.GameVersion} no Longer Supported, Minimum Supported Version: {MinimumVersion}";
                RestAPI.Write(response, SharpHttpCode.Gone, text);
                return;
            }

            var list = Query();

            var info = new MasterServerInfoResponse(list);

            RestAPI.Write(response, info);
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
        static void RegisterServer(SharpHttpRequest request, SharpHttpResponse response)
        {
            RegisterGameServerRequest payload;
            try
            {
                RestAPI.Read(request, out payload);
            }
            catch (Exception)
            {
                RestAPI.Write(response, SharpHttpCode.NotAcceptable, $"Error Reading Register Request");
                return;
            }

            var result = RegisterServer(payload);
            RestAPI.Write(response, result);
        }

        static RegisterGameServerResult RegisterServer(RegisterGameServerRequest request)
        {
            if (request.Key != ApiKey.Token)
            {
                Log.Info($"Server {request.ID} Trying to Register With Invalid API Key");
                return new RegisterGameServerResult(false);
            }

            RegisterServer(request.ID, request.Region);
            return new RegisterGameServerResult(true);
        }

        static GameServer RegisterServer(GameServerID id, GameServerRegion region)
        {
            var server = new GameServer(id, region);

            lock (SyncLock)
            {
                Servers[id] = server;
            }

            Log.Info($"Registering Server: {server}");

            return server;
        }
        #endregion

        #region Remove Server
        static void RemoveServer(SharpHttpRequest request, SharpHttpResponse response)
        {
            RemoveGameServerRequest payload;
            try
            {
                RestAPI.Read(request, out payload);
            }
            catch (Exception)
            {
                RestAPI.Write(response, SharpHttpCode.NotAcceptable, $"Error Reading Remove Request");
                return;
            }

            RemoveServer(payload.ID);

            var result = new RemoveGameServerResult(true);
            RestAPI.Write(response, result);
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

    [Serializable]
    public struct GameServer
    {
        GameServerID id;
        public GameServerID ID => id;

        GameServerRegion region;
        public GameServerRegion Region => region;

        public GameServerInfo GetInfo() => new GameServerInfo(id, region);
        public static GameServerInfo GetInfo(GameServer server) => server.GetInfo();

        public override string ToString() => $"{id} | {region}";

        public GameServer(GameServerID id, GameServerRegion region)
        {
            this.id = id;
            this.region = region;
        }
    }
}