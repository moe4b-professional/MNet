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
        public static RestClientAPI Rest { get; private set; }

        public static ushort Port => Constants.Server.Master.Rest.Port;
        public static RestScheme Scheme => GameServer.Config.RestScheme;

        public static void Configure(IPAddress address)
        {
            Rest = new RestClientAPI(Port, Scheme);
            Rest.SetIP(address);
        }

        public static bool Register(GameServerInfo info, string key, out RegisterGameServerResponse response)
        {
            var payload = new RegisterGameServerRequest(info, key);

            try
            {
                response = Rest.POST<RegisterGameServerRequest, RegisterGameServerResponse>(Constants.Server.Master.Rest.Requests.Server.Register, payload).Result;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                response = default;
                return false;
            }
        }

        public static bool Remove(GameServerID id, string key, out RemoveGameServerResponse response)
        {
            var payload = new RemoveGameServerRequest(id, key);

            try
            {
                response = Rest.POST<RemoveGameServerRequest, RemoveGameServerResponse>(Constants.Server.Master.Rest.Requests.Server.Remove, payload).Result;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                response = default;
                return false;
            }
        }
    }
}