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
        public IPAddress PublicAddress { get; protected set; }

        [JsonProperty]
        public IPAddress MasterAddress { get; protected set; }

        [JsonProperty]
        public NetworkTransportType NetworkTransport { get; protected set; }

        [JsonProperty]
        public string Version { get; protected set; }

        protected override void WriteDefaults()
        {
            base.WriteDefaults();

            PublicAddress = IPAddress.Any;
            MasterAddress = IPAddress.Any;
            NetworkTransport = NetworkTransportType.WebSocketSharp;
            Version = "0.0.0";
        }

        public Config() { }
    }
}