﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Text;

using Newtonsoft.Json;

namespace MNet
{
    [Serializable]
    [JsonObject]
    public partial class Config : Config<Config>
    {
        [JsonProperty]
        public NetworkTransportType Transport { get; protected set; }

        [JsonProperty]
        public AppConfig[] Apps { get; protected set; }

        protected override void WriteDefaults()
        {
            base.WriteDefaults();

            Transport = NetworkTransportType.WebSockets;

            Apps = new AppConfig[] { };
        }

        public Config() { }
    }
}