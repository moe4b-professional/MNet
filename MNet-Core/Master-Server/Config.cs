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
        public Version MinimumVersion { get; protected set; }

        protected override void WriteDefaults()
        {
            base.WriteDefaults();

            MinimumVersion = Version.Zero;
        }

        public Config() { }
    }
}