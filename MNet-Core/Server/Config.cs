using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace MNet
{
    public abstract class Config<T>
        where T : Config<T>, new()
    {
        public const string FileName = "Config.json";

        protected virtual void WriteDefaults()
        {
            
        }

        public Config()
        {

        }

        //Static
        private static JsonSerializerSettings SerializerSettings { get; set; }

        public static T Read()
        {
            var instance = new T();

            instance.WriteDefaults();

            var json = LoadJson();

            JsonConvert.PopulateObject(json, instance, SerializerSettings);

            return instance;
        }

        private static string LoadJson()
        {
            if (File.Exists(FileName) == false) return null;

            return File.ReadAllText(FileName);
        }

        static Config()
        {
            SerializerSettings = new JsonSerializerSettings();
            SerializerSettings.Converters.Add(IPAddressConverter.Instance);
            SerializerSettings.Converters.Add(VersionConverter.Instance);
        }
    }

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

            return IPAddress.Parse(text);
        }
    }

    class VersionConverter : JsonConverter
    {
        public static VersionConverter Instance { get; private set; } = new VersionConverter();

        public override bool CanConvert(Type objectType) => objectType == typeof(Version);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var text = value.ToString();

            writer.WriteValue(text);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var text = (string)reader.Value;

            return Version.Parse(text);
        }
    }
}