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
    public struct RoomInfo : INetworkSerializable
    {
        RoomID id;
        public RoomID ID { get { return id; } }

        string name;
        public string Name { get { return name; } }

        byte capacity;
        public byte Capacity { get { return capacity; } }

        byte occupancy;
        public byte Occupancy { get { return occupancy; } }

        bool visibile;
        public bool Visibile => visibile;

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref id);
            context.Select(ref name);

            context.Select(ref capacity);
            context.Select(ref occupancy);

            context.Select(ref visibile);

            context.Select(ref attributes);
        }

        public RoomInfo(RoomID id, string name, byte capacity, byte occupancy, bool visibile, AttributesCollection attributes)
        {
            this.id = id;
            this.name = name;

            this.capacity = capacity;
            this.occupancy = occupancy;

            this.visibile = visibile;

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

    public struct RoomID : INetworkSerializable
    {
        uint value;
        public uint Value { get { return value; } }

        public void Select(ref NetworkSerializationContext context)
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
}