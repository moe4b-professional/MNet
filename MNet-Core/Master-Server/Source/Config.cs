using System;
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
        public AppConfiguration[] Apps { get; protected set; }

        protected override void WriteDefaults()
        {
            base.WriteDefaults();
        }

        public Config() { }
    }
}