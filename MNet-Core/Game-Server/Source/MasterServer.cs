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

        public static bool Register(GameServerInfo info, string key, out RegisterGameServerResponse response)
        {
            var payload = new RegisterGameServerRequest(info, key);

            if (PUT(payload, Constants.Server.Master.Rest.Requests.Server.Register, out response) == false)
                return false;

            return true;
        }

        public static bool Remove(GameServerID id, string key)
        {
            var payload = new RemoveGameServerRequest(id, key);

            return PUT(payload, Constants.Server.Master.Rest.Requests.Server.Remove, out RemoveGameServerResponse response);
        }

        static bool PUT<TPayload, TResult>(TPayload payload, string path, out TResult result)
        {
            var content = RestAPI.WriteContent(payload);

            HttpResponseMessage response;

            try
            {
                response = Client.PutAsync(URL + path, content).Result;
            }
            catch (Exception ex)
            {
                Log.Error($"{path}: {ex.Message}");

                result = default;
                return false;
            }

            if (response.IsSuccessStatusCode == false)
            {
                var code = (RestStatusCode)response.StatusCode;

                Log.Error($"{path}: {code}");

                result = default;
                return false;
            }

            result = RestAPI.Read<TResult>(response);
            return true;
        }

        static MasterServer()
        {
            Client = new HttpClient();
        }
    }
}