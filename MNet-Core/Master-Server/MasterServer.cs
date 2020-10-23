using System;
using System.Net;
using System.Collections.Generic;

using SharpHttpCode = WebSocketSharp.Net.HttpStatusCode;
using SharpHttpRequest = WebSocketSharp.Net.HttpListenerRequest;
using SharpHttpResponse = WebSocketSharp.Net.HttpListenerResponse;

using System.Threading;
using System.Linq;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MNet
{
    static class MasterServer
    {
        public static Config Config { get; private set; }

        public static Version MinimumVersion => Config.MinimumVersion;

        public static RestAPI Rest { get; private set; }

        public static Dictionary<GameServerID, GameServer> Servers { get; private set; }

        static readonly object SyncLock = new object();

        static void Main()
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

        static GameServerInfo[] Query()
        {
            lock (SyncLock)
            {
                var list = Servers.ToArray(GameServer.GetInfo);

                return list;
            }
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

            RegisterServer(request.Info);
            return new RegisterGameServerResult(true);
        }

        static GameServer RegisterServer(GameServerInfo info)
        {
            var server = new GameServer(info);

            lock (SyncLock)
            {
                Servers[info.ID] = server;
            }

            Log.Info($"Registering Server: [{server}]");

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
        GameServerInfo info;
        public GameServerInfo Info => info;

        public GameServerID ID => info.ID;
        public string Name => info.Name;
        public GameServerRegion Region => info.Region;

        public static GameServerInfo GetInfo(GameServer server) => server.Info;

        public override string ToString() => info.ToString();

        public GameServer(GameServerInfo info)
        {
            this.info = info;
        }
    }
}