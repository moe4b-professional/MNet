using System;
using System.Net;
using System.Collections.Generic;

using SharpHttpRequest = WebSocketSharp.Net.HttpListenerRequest;
using SharpHttpResponse = WebSocketSharp.Net.HttpListenerResponse;
using SharpHttpCode = WebSocketSharp.Net.HttpStatusCode;
using System.Threading;
using System.Linq;

namespace Backend
{
    static class MasterServer
    {
        public static RestAPI Rest { get; private set; }

        public static Dictionary<GameServerID, GameServer> Servers { get; private set; }

        static void Main(string[] args)
        {
            Console.Title = "Master Sever";

            ApiKey.Read();

            Rest = new RestAPI(Constants.Server.Master.Rest.Port);
            Rest.Start();

            Rest.Router.Register(Constants.Server.Master.Rest.Requests.Info, GetInfo);
            Rest.Router.Register(Constants.Server.Master.Rest.Requests.Server.Register, RegisterServer);
            Rest.Router.Register(Constants.Server.Master.Rest.Requests.Server.Remove, RemoveServer);

            while (true) Console.ReadLine();
        }

        static void GetInfo(SharpHttpRequest request, SharpHttpResponse response)
        {
            var array = Servers.ToArray(GameServer.GetInfo);

            var payload = new MasterServerInfoPayload(array);

            RestAPI.WriteTo(response, payload);
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

            var result = RegisterServer(payload.ID, payload.Region, payload.Key);
            RestAPI.WriteTo(response, result);
        }

        static RegisterGameServerResult RegisterServer(GameServerID id, GameServerRegion region, string key)
        {
            if (key != ApiKey.Token)
            {
                Log.Info($"Server {id} Trying to Register With Invalid API Key");
                return new RegisterGameServerResult(false);
            }
            
            RegisterServer(id, region);
            return new RegisterGameServerResult(true);
        }

        static GameServer RegisterServer(GameServerID id, GameServerRegion region)
        {
            var server = new GameServer(id, region);

            Servers[id] = server;

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

        static bool RemoveServer(GameServerID id) => Servers.Remove(id);
        #endregion

        static MasterServer()
        {
            Servers = new Dictionary<GameServerID, GameServer>();
        }
    }

    class GameServer
    {
        public GameServerID ID { get; protected set; }

        public GameServerRegion Region { get; protected set; }

        public GameServerInfo GetInfo() => new GameServerInfo(ID, Region);
        public static GameServerInfo GetInfo(GameServer server) => server.GetInfo();

        public GameServer(GameServerID id, GameServerRegion region)
        {
            this.ID = id;
            this.Region = region;
        }
    }
}