using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Text;

using Newtonsoft.Json;

namespace Backend
{
    [JsonObject]
    public partial class Config : Config<Config>
    {
        [JsonProperty]
        public IPAddress PublicAddress { get; protected set; }

        [JsonProperty]
        public IPAddress MasterAddress { get; protected set; }

        public override void WriteDefaults()
        {
            PublicAddress = IPAddress.Any;

            MasterAddress = IPAddress.Any;
        }

        public Config() { }
    }
}