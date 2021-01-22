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
        public static RestScheme Scheme => GameServer.Config.Local.RestScheme;

        public static void Configure()
        {
            Rest = new RestClientAPI(Port, Scheme);
            Rest.SetIP(GameServer.Config.Local.MasterAddress);
        }

        public static void Register()
        {
            var info = GameServer.Info.Read();

            var payload = new RegisterGameServerRequest(info, ApiKey.Token);

            RegisterGameServerResponse response;

            try
            {
                response = Rest.POST<RegisterGameServerRequest, RegisterGameServerResponse>(Constants.Server.Master.Rest.Requests.Server.Register, payload).Result;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return;
            }

            GameServer.Config.Set(response.RemoteConfig);

            AppsAPI.Set(response.Apps);
        }

        public static bool Remove()
        {
            var payload = new RemoveGameServerRequest(GameServer.Info.ID, ApiKey.Token);

            try
            {
                var response = Rest.POST<RemoveGameServerRequest, RemoveGameServerResponse>(Constants.Server.Master.Rest.Requests.Server.Remove, payload).Result;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return false;
            }
        }
    }
}