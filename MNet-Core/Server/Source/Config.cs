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

        protected virtual void Validate()
        {

        }

        public Config()
        {

        }

        //Static Utility
        private static JsonSerializerSettings SerializerSettings { get; set; }

        public static T Read()
        {
            var instance = new T();

            instance.WriteDefaults();

            var json = LoadJson();
            if (json == null)
                Log.Error($"No {FileName} File Found");
            else
                JsonConvert.PopulateObject(json, instance, SerializerSettings);

            instance.Validate();

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
}