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
        Local = 0,
        USA = 1, //USA #! YEAH, MURICA!
        Europe = 2,
        Asia = 3
    }

    [Preserve]
    [Serializable]
    public struct GameServerID : INetworkSerializable
    {
        IPAddress value;
        public IPAddress Value { get { return value; } }

        public string Address => ToString();

        public void Select(ref NetworkSerializationContext context)
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

        GameServerRegion region;
        public GameServerRegion Region => region;

        ushort occupancy;
        public int Occupancy => occupancy;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref id);
            context.Select(ref region);
            context.Select(ref occupancy);
        }

        public override string ToString() => $"[ {id} | {region} ]";

        public GameServerInfo(GameServerID id, GameServerRegion region, ushort occupancy)
        {
            this.id = id;
            this.region = region;
            this.occupancy = occupancy;
        }
    }
}