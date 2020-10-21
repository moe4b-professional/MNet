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
        public Version[] Versions { get; protected set; }

        protected override void WriteDefaults()
        {
            base.WriteDefaults();

            PublicAddress = IPAddress.Any;
            MasterAddress = IPAddress.Any;
            NetworkTransport = NetworkTransportType.WebSocketSharp;
            Versions = new Version[] { Version.Zero };
        }

        public Config() { }
    }
}