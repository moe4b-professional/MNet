using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
    [Serializable]
    public struct RoomBasicInfo : INetworkSerializable
    {
        RoomID id;
        public RoomID ID { get { return id; } }

        string name;
        public string Name { get { return name; } }

        byte capacity;
        public byte Capacity { get { return capacity; } }

        byte occupancy;
        public byte Occupancy { get { return occupancy; } }

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
            context.Select(ref name);

            context.Select(ref capacity);
            context.Select(ref occupancy);

            context.Select(ref attributes);
        }

        public RoomBasicInfo(RoomID id, string name, byte capacity, byte occupancy, AttributesCollection attributes)
        {
            this.id = id;
            this.name = name;

            this.capacity = capacity;
            this.occupancy = occupancy;

            this.attributes = attributes;
        }

        public override string ToString()
        {
            return "ID: " + ID + Environment.NewLine +
                "Name: " + Name + Environment.NewLine +
                "MaxPlayers: " + Capacity + Environment.NewLine +
                "PlayersCount: " + Occupancy + Environment.NewLine;
        }
    }

    [Preserve]
    [Serializable]
    public struct RoomInnerInfo : INetworkSerializable
    {
        byte tickLatency;
        public byte TickLatency => tickLatency;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref tickLatency);
        }

        public RoomInnerInfo(byte tickRate)
        {
            this.tickLatency = tickRate;
        }
    }

    [Preserve]
    public struct RoomInfo : INetworkSerializable
    {
        RoomBasicInfo basic;
        public RoomBasicInfo Basic => basic;

        RoomInnerInfo inner;
        public RoomInnerInfo Inner => inner;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref basic);
            context.Select(ref inner);
        }

        public RoomInfo(RoomBasicInfo basic, RoomInnerInfo inner)
        {
            this.basic = basic;
            this.inner = inner;
        }
    }

    public struct RoomID : INetworkSerializable
    {
        uint value;
        public uint Value { get { return value; } }

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref value);
        }

        public RoomID(uint value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(RoomID))
            {
                var target = (RoomID)obj;

                return target.value == this.value;
            }

            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(RoomID a, RoomID b) => a.Equals(b);
        public static bool operator !=(RoomID a, RoomID b) => !a.Equals(b);

        public static RoomID Increment(RoomID id) => new RoomID(id.value + 1);

        public static bool TryParse(string text, out RoomID id)
        {
            if(uint.TryParse(text, out var value) == false)
            {
                id = default;
                return false;
            }

            id = new RoomID(value);
            return true;
        }
    }
}