using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Backend
{
    [Serializable]
    public struct GameServerID : INetworkSerializable
    {
        IPAddress value;
        public IPAddress Value { get { return value; } }

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref value);
        }

        public GameServerID(IPAddress value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(GameServerID))
            {
                var target = (GameServerID)obj;

                return target.value == this.value;
            }

            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(GameServerID a, GameServerID b) => a.Equals(b);
        public static bool operator !=(GameServerID a, GameServerID b) => !a.Equals(b);
    }

    [Serializable]
    public enum GameServerRegion : byte
    {
        US, EU
    }

    [Serializable]
    public struct GameServerInfo : INetworkSerializable
    {
        GameServerID id;
        public GameServerID ID => id;

        GameServerRegion region;
        public GameServerRegion Region => region;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
            context.Select(ref region);
        }

        public GameServerInfo(GameServerID id, GameServerRegion region)
        {
            this.id = id;
            this.region = region;
        }
    }
}