using System;
using System.Net;
using System.Text;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace MNet
{
    class IPAddressConverter : JsonConverter
    {
        public static IPAddressConverter Instance { get; private set; } = new IPAddressConverter();

        public override bool CanConvert(Type type) => typeof(IPAddress).IsAssignableFrom(type);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var text = value.ToString();

            writer.WriteValue(text);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var text = (string)reader.Value;

            if (text == null) return null;

            if (text == "localhost") return IPAddress.Loopback;

            return IPAddress.Parse(text);
        }
    }
}