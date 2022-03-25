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

        public static async Task Register()
        {
            var info = GameServer.Info.Read();
            var payload = new RegisterGameServerRequest(info, ApiKey.Token);

            var response = await Rest.POST<RegisterGameServerResponse>(Constants.Server.Master.Rest.Requests.Server.Register, payload);

            Log.Info(response.RemoteConfig);

            GameServer.Config.Set(response.RemoteConfig);
            AppsAPI.Set(response.Apps);
        }

        public static async Task Unregister()
        {
            var payload = new RemoveGameServerRequest(GameServer.Info.ID, ApiKey.Token);

            var response = await Rest.POST<RemoveGameServerResponse>(Constants.Server.Master.Rest.Requests.Server.Unregister, payload);

            AppsAPI.Clear();
        }
    }
}