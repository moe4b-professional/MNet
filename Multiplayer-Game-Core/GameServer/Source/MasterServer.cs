﻿using System;
using System.Text;
using System.Collections.Generic;

using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading.Tasks;

namespace Backend
{
    static class MasterServer
    {
        public static IPAddress Address { get; private set; }
        public static ushort Port => Constants.MasterServer.Rest.Port;
        public static RestScheme Scheme { get; private set; } = RestScheme.HTTP;
        public static string URL => $"{Scheme}://{Address}:{Port}";

        public static void Configure(IPAddress address)
        {
            MasterServer.Address = address;
        }

        public static HttpClient Client { get; private set; }

        public static RegisterGameServerResult Register(GameServerID id, GameServerRegion region, string key)
        {
            var request = new RegisterGameServerRequest(id, region, key);
            var content = RestAPI.WriteContent(request);

            var response = Client.PutAsync(URL + Constants.MasterServer.Rest.Requests.Server.Register, content).Result;

            var result = RestAPI.Read<RegisterGameServerResult>(response);

            Log.Info($"Register Server: {result.Success}");

            return result;
        }

        public static RemoveGameSeverResult Remove(GameServerID id, string key)
        {
            var request = new RemoveGameSeverRequest(id, key);
            var content = RestAPI.WriteContent(request);

            var response = Client.PostAsync(URL + Constants.MasterServer.Rest.Requests.Server.Remove, content).Result;

            var result = RestAPI.Read<RemoveGameSeverResult>(response);

            Log.Info($"Remove Server: {result.Success}");

            return result;
        }

        static MasterServer()
        {
            Client = new HttpClient();
        }
    }
}