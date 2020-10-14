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

        public static RestAPI Rest { get; private set; }

        public static Dictionary<GameServerID, GameServerInfo> Servers { get; private set; }

        static void Main(string[] args)
        {
            Console.Title = "Master Sever";

            ApiKey.Read();

            Config = Config.Read();

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
                payload = RestAPI.Read<MasterServerInfoRequest>(request);
            }
            catch (Exception)
            {
                RestAPI.WriteTo(response, SharpHttpCode.NotAcceptable, $"Error Reading Info Request");
                return;
            }

            var list = new List<GameServerInfo>();

            foreach (var server in Servers.Values)
            {
                if (payload.Version != server.Version) continue;

                list.Add(server);
            }

            var info = new MasterServerInfoResponse(list);

            RestAPI.WriteTo(response, info);
        }

        #region Register Server
        static void RegisterServer(SharpHttpRequest request, SharpHttpResponse response)
        {
            RegisterGameServerRequest payload;
            try
            {
                payload = RestAPI.Read<RegisterGameServerRequest>(request);
            }
            catch (Exception)
            {
                RestAPI.WriteTo(response, SharpHttpCode.NotAcceptable, $"Error Reading Register Request");
                return;
            }

            var result = RegisterServer(payload);
            RestAPI.WriteTo(response, result);
        }

        static RegisterGameServerResult RegisterServer(RegisterGameServerRequest request)
        {
            return RegisterServer(request.ID, request.Version, request.Region, request.Key);
        }

        static RegisterGameServerResult RegisterServer(GameServerID id, string version, GameServerRegion region, string key)
        {
            if (key != ApiKey.Token)
            {
                Log.Info($"Server {id} Trying to Register With Invalid API Key");
                return new RegisterGameServerResult(false);
            }

            RegisterServer(id, version, region);
            return new RegisterGameServerResult(true);
        }

        static GameServerInfo RegisterServer(GameServerID id, string version, GameServerRegion region)
        {
            var server = new GameServerInfo(id, version, region);

            Servers[id] = server;

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
                payload = RestAPI.Read<RemoveGameServerRequest>(request);
            }
            catch (Exception)
            {
                RestAPI.WriteTo(response, SharpHttpCode.NotAcceptable, $"Error Reading Remove Request");
                return;
            }

            RemoveServer(payload.ID);

            var result = new RemoveGameServerResult(true);
            RestAPI.WriteTo(response, result);
        }

        static bool RemoveServer(GameServerID id)
        {
            if (Servers.Remove(id))
            {
                Log.Info($"Removing Server: {id}");
                return true;
            }

            return false;
        }
        #endregion

        static MasterServer()
        {
            Servers = new Dictionary<GameServerID, GameServerInfo>();
        }
    }
}