using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Backend
{
    public abstract class Config<T>
        where T : Config<T>, new()
    {
        public const string FileName = "Config.json";

        public abstract void WriteDefaults();

        public Config()
        {

        }

        //Static
        public static JsonSerializerSettings SerializerSettings { get; protected set; }

        public static T Read()
        {
            var instance = new T();

            instance.WriteDefaults();

            var json = LoadJson();

            JsonConvert.PopulateObject(json, instance, SerializerSettings);

            return instance;
        }

        public static string LoadJson()
        {
            if (File.Exists(FileName) == false) return null;

            return File.ReadAllText(FileName);
        }

        static Config()
        {
            SerializerSettings = new JsonSerializerSettings();
            SerializerSettings.Converters.Add(new IPAddressConverter());
        }
    }

    class IPAddressConverter : JsonConverter
    {
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
}
