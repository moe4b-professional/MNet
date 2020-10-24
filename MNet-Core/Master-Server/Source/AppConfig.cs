using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace MNet
{
    [Serializable]
    [JsonObject]
    public class AppConfiguration
    {
        [JsonProperty]
        public AppID ID { get; protected set; }

        [JsonProperty]
        public Version MinimumVersion { get; protected set; }

        public override string ToString() => $"[ {ID} | v{MinimumVersion} ]";

        public static AppID SelectID(AppConfiguration data) => data.ID;
    }
}