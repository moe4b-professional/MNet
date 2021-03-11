using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Text;

using Newtonsoft.Json;

namespace MNet
{
    [JsonObject]
    public partial class LocalConfig : LocalConfig<LocalConfig>
    {
        [JsonProperty]
        public string MasterAddress { get; protected set; }

        [JsonProperty]
        public IPAddress PersonalAddress { get; protected set; }

        [JsonProperty]
        public GameServerRegion Region { get; protected set; }

        [JsonProperty]
        public RestScheme RestScheme { get; protected set; }

        public RemoteConfig Remote { get; protected set; }

        protected override void WriteDefaults()
        {
            base.WriteDefaults();

            PersonalAddress = null;
            MasterAddress = null;

            Region = GameServerRegion.Local;

            RestScheme = RestScheme.HTTP;
        }

        public virtual void Set(RemoteConfig instance) => Remote = instance;

        public LocalConfig() { }
    }
}