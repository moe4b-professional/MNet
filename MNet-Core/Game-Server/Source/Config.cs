using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Text;

using Newtonsoft.Json;

namespace MNet
{
    [JsonObject]
    public partial class Config : Config<Config>
    {
        [JsonProperty]
        public string Name { get; protected set; }

        [JsonProperty]
        public IPAddress PublicAddress { get; protected set; }

        [JsonProperty]
        public IPAddress MasterAddress { get; protected set; }

        [JsonProperty]
        public GameServerRegion Region { get; protected set; }

        [JsonProperty]
        public RestScheme RestScheme { get; protected set; }

        [JsonProperty]
        public NetworkTransportType NetworkTransport { get; protected set; }

        protected override void WriteDefaults()
        {
            base.WriteDefaults();

            Name = "Game Server";

            PublicAddress = null;
            MasterAddress = null;

            Region = GameServerRegion.Local;

            RestScheme = RestScheme.HTTP;

            NetworkTransport = NetworkTransportType.WebSocketSharp;
        }

        public Config() { }
    }
}