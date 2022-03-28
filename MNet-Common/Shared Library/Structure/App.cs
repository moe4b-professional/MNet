using System;
using System.Text;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace MNet
{
    [Preserve]
    [Serializable]
    public struct AppID : INetworkSerializable
    {
        string value;
        public string Value { get { return value; } }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref value);
        }

        public AppID(string value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(AppID))
            {
                var target = (AppID)obj;

                return Equals(target.value, this.value);
            }

            return false;
        }

        public override int GetHashCode() => value == null ? 0 : value.GetHashCode();

        public override string ToString() => value?.ToString();

        public static bool operator ==(AppID a, AppID b) => a.Equals(b);
        public static bool operator !=(AppID a, AppID b) => !a.Equals(b);

        public static explicit operator AppID(string text) => new AppID(text);
    }

    [Preserve]
    [JsonObject]
    [Serializable]
    public struct AppConfig : INetworkSerializable
    {
        AppID id;
        [JsonProperty]
        public AppID ID
        {
            get => id;
            private set => id = value;
        }

        NetworkTransportType transport;
        [JsonProperty]
        public NetworkTransportType Transport
        {
            get => transport;
            private set => transport = value;
        }

        Version minimumVersion;
        [JsonProperty]
        public Version MinimumVersion
        {
            get => minimumVersion;
            private set => minimumVersion = value;
        }

        byte tickDelay;
        [JsonProperty]
        public byte TickDelay
        {
            get => tickDelay;
            private set => tickDelay = value;
        }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref id);
            context.Select(ref transport);
            context.Select(ref minimumVersion);
            context.Select(ref tickDelay);
        }

        public override string ToString() => $"[ {id} | {transport} | v{minimumVersion} | {tickDelay}ms ]";

        public AppConfig(AppID id, NetworkTransportType transport, Version minimumVersion, byte tickDelay)
        {
            this.id = id;
            this.transport = transport;
            this.minimumVersion = minimumVersion;
            this.tickDelay = tickDelay;
        }

        //Static Utility
        public static AppID SelectID(AppConfig confiuration) => confiuration.id;
    }
}