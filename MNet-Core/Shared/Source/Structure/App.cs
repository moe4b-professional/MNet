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
        Guid value;
        public Guid Value { get { return value; } }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref value);
        }

        public AppID(Guid value)
        {
            this.value = value;
        }

        public static AppID Parse(string text)
        {
            var value = Guid.Parse(text);

            return new AppID(value);
        }
        public static bool TryParse(string text, out AppID id)
        {
            if (Guid.TryParse(text, out var value))
            {
                id = new AppID(value);
                return true;
            }

            id = default;
            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(AppID))
            {
                var target = (AppID)obj;

                return target.value == this.value;
            }

            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString().ToUpper();

        public static bool operator ==(AppID a, AppID b) => a.Equals(b);
        public static bool operator !=(AppID a, AppID b) => !a.Equals(b);

        public static explicit operator AppID(string text) => Parse(text);
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

        bool queueMessages;
        [JsonProperty]
        public bool QueueMessages
        {
            get => queueMessages;
            private set => queueMessages = value;
        }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref id);
            context.Select(ref minimumVersion);
            context.Select(ref tickDelay);
            context.Select(ref queueMessages);
        }

        public override string ToString() => $"[ {id} | v{minimumVersion} | {tickDelay}ms | Queue Messages: {queueMessages} ]";

        public AppConfig(AppID id, Version minimumVersion, byte tickDelay, bool queueMessages)
        {
            this.id = id;
            this.minimumVersion = minimumVersion;
            this.tickDelay = tickDelay;
            this.queueMessages = queueMessages;
        }

        //Static Utility
        public static AppID SelectID(AppConfig confiuration) => confiuration.id;
    }
}