using System;
using System.Text;
using System.Collections.Generic;

using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading.Tasks;

namespace MNet
{
    static class MasterServer
    {
        public static IPAddress Address { get; private set; }
        public static ushort Port => Constants.Server.Master.Rest.Port;

        public static RestScheme Scheme => GameServer.Config.RestScheme;

        public static string URL => $"{Scheme}://{Address}:{Port}";

        public static void Configure(IPAddress address)
        {
            MasterServer.Address = address;
        }

        public static HttpClient Client { get; private set; }

        public static RegisterGameServerResult Register(GameServerInfo info, string key)
        {
            var request = new RegisterGameServerRequest(info, key);
            var content = RestAPI.WriteContent(request);

            HttpResponseMessage response;

            try
            {
                response = Client.PutAsync(URL + Constants.Server.Master.Rest.Requests.Server.Register, content).Result;
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}");
                return null;
            }

            var result = RestAPI.Read<RegisterGameServerResult>(response);

            Log.Info($"Register Server: {result.Success}");

            return result;
        }

        public static RemoveGameServerResult Remove(GameServerID id, string key)
        {
            var request = new RemoveGameServerRequest(id, key);
            var content = RestAPI.WriteContent(request);

            HttpResponseMessage response;

            try
            {
                response = Client.PostAsync(URL + Constants.Server.Master.Rest.Requests.Server.Remove, content).Result;
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}");
                return null;
            }

            var result = RestAPI.Read<RemoveGameServerResult>(response);

            Log.Info($"Remove Server: {result.Success}");

            return result;
        }

        static MasterServer()
        {
            Client = new HttpClient();
        }
    }
}