﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace MNet
{
    [Serializable]
    public enum GameServerRegion : byte
    {
        Local, USA, Europe, Asia
    }

    [Preserve]
    [Serializable]
    public struct GameServerID : INetworkSerializable
    {
        IPAddress value;
        public IPAddress Value { get { return value; } }

        public string Address => ToString();

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref value);
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(GameServerID))
            {
                var target = (GameServerID)obj;

                return Equals(this.value, target.value);
            }

            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public GameServerID(IPAddress value)
        {
            this.value = value;
        }

        public static GameServerID Parse(string address)
        {
            var ip = IPAddress.Parse(address);

            return new GameServerID(ip);
        }

        public static bool operator ==(GameServerID a, GameServerID b) => a.Equals(b);
        public static bool operator !=(GameServerID a, GameServerID b) => !a.Equals(b);

        public static bool Equals(IPAddress a, IPAddress b) => a.Equals(b);
    }

    [Preserve]
    [Serializable]
    public struct GameServerInfo : INetworkSerializable
    {
        GameServerID id;
        public GameServerID ID => id;

        string name;
        public string Name => name;

        GameServerRegion region;
        public GameServerRegion Region => region;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
            context.Select(ref name);
            context.Select(ref region);
        }

        public override string ToString() => $"[ {name} | {id} | {region} ]";

        public GameServerInfo(GameServerID id, string name, GameServerRegion region)
        {
            this.id = id;
            this.name = name;
            this.region = region;
        }
    }
}