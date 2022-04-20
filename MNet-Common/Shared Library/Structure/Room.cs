using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
    [Serializable]
    public struct RoomInfo : INetworkSerializable
    {
        RoomID id;
        public RoomID ID { get { return id; } }

        FixedString32 name;
        public FixedString32 Name { get { return name; } }

        byte capacity;
        public byte Capacity { get { return capacity; } }

        byte occupancy;
        public byte Occupancy { get { return occupancy; } }

        bool visibile;
        public bool Visibile => visibile;

        bool locked;
        public bool Locked => locked;

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref id);
            context.Select(ref name);

            context.Select(ref capacity);
            context.Select(ref occupancy);

            context.Select(ref visibile);
            context.Select(ref locked);

            context.Select(ref attributes);
        }

        public RoomInfo(RoomID id, FixedString32 name, byte capacity, byte occupancy, bool visibile, bool locked, AttributesCollection attributes)
        {
            this.id = id;
            this.name = name;

            this.capacity = capacity;
            this.occupancy = occupancy;

            this.visibile = visibile;
            this.locked = locked;

            this.attributes = attributes;
        }

        public override string ToString()
        {
            return $"ID: {id}\n" +
                $"Name: {name}\n" +
                $"Capacity: {capacity}\n" +
                $"Occupancy: {occupancy}";
        }
    }

    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RoomID : IEquatable<RoomID>
    {
        uint value;
        public uint Value { get { return value; } }

        public RoomID(uint value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is RoomID target)
                return Equals(target);

            return false;
        }
        public bool Equals(RoomID id) => this.value == id.value;

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        //Static Utility

        public static RoomID Min { get; private set; } = new RoomID(ushort.MinValue);
        public static RoomID Max { get; private set; } = new RoomID(ushort.MaxValue);

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

    public enum MigrationPolicy : byte
    {
        /// <summary>
        /// Continues the Room by choosing a random client as Master
        /// </summary>
        Continue,
        /// <summary>
        /// Stops the Room and disconnects all clients
        /// </summary>
        Stop,
    }

    public struct RoomOptions : INetworkSerializable
    {
        public FixedString32 Name;

        public byte Capacity;

        public bool Visible;
        public FixedString16 Password;

        public MigrationPolicy MigrationPolicy;

        public AttributesCollection Attributes;

        public byte? Scene;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref Name);

            context.Select(ref Capacity);

            context.Select(ref Visible);
            context.Select(ref Password);

            context.Select(ref MigrationPolicy);
            context.Select(ref Attributes);

            context.Select(ref Scene);
        }

        public static RoomOptions Default { get; } = new RoomOptions()
        {
            Name = new FixedString32("Default Game Room"),
            Capacity = 10,
            Visible = false,
            Password = default,
            MigrationPolicy = MigrationPolicy.Stop,
            Attributes = null,
            Scene = null,
        };
    }
}