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
    public partial class LocalConfig : LocalConfig<LocalConfig>
    {
        [JsonProperty]
        public RestScheme RestScheme { get; protected set; }

        [JsonProperty]
        public AppConfig[] Apps { get; protected set; }

        protected override void WriteDefaults()
        {
            base.WriteDefaults();

            RestScheme = RestScheme.HTTP;

            Apps = new AppConfig[] { };
        }

        public RemoteConfig GetRemoteConfig() => new RemoteConfig();

        public LocalConfig() { }
    }
}