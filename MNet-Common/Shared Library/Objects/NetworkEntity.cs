using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    public enum EntityType : byte
    {
        /// <summary>
        /// Entities local to a Scene
        /// </summary>
        SceneObject,

        /// <summary>
        /// Dynamically Created Entities such as Players
        /// </summary>
        Dynamic,

        /// <summary>
        /// Dynamic Entities that Have no Owner anymore, will be controlled by Master Client
        /// </summary>
        Orphan,
    }

    [Flags]
    public enum PersistanceFlags : byte
    {
        /// <summary>
        /// Entity Will Not Persist
        /// </summary>
        None = 0,
        /// <summary>
        /// Entity Will Persist Through Player Disconnection
        /// </summary>
        PlayerDisconnection = 1 << 1,
        /// <summary>
        /// Entity Will Persist Through Single Scene Loading
        /// </summary>
        SceneLoad = 1 << 2,
    }

    [Preserve]
    [Serializable]
    public struct NetworkEntityID : IManualNetworkSerializable, IEquatable<NetworkEntityID>
    {
        ushort value;
        public ushort Value { get { return value; } }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(value);
        }

        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out value);
        }

        public NetworkEntityID(ushort value)
        {
            this.value = value;
        }
        NetworkEntityID(int value) : this((ushort)value) { }

        public override bool Equals(object obj)
        {
            if (obj is NetworkEntityID target)
                return Equals(target);

            return false;
        }
        public bool Equals(NetworkEntityID target) => this.value == target.value;

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        //Static Utility

        public static NetworkEntityID Min { get; private set; } = new NetworkEntityID(ushort.MinValue);
        public static NetworkEntityID Max { get; private set; } = new NetworkEntityID(ushort.MaxValue);

        public static bool operator ==(NetworkEntityID a, NetworkEntityID b) => a.Equals(b);
        public static bool operator !=(NetworkEntityID a, NetworkEntityID b) => !a.Equals(b);

        public static NetworkEntityID Increment(NetworkEntityID id) => new NetworkEntityID(id.value + 1);
    }

    [Preserve]
    public struct EntitySpawnToken : IManualNetworkSerializable, IEquatable<EntitySpawnToken>
    {
        byte value;
        public byte Value => value;

        public void Serialize(NetworkWriter writer)
        {
            writer.Insert(value);
        }

        public void Deserialize(NetworkReader reader)
        {
            value = reader.TakeByte();
        }

        public EntitySpawnToken(byte value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is EntitySpawnToken target)
                return Equals(target);

            return false;
        }
        public bool Equals(EntitySpawnToken target) => this.value == target.value;

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        //Static Utility

        public static EntitySpawnToken Min { get; private set; } = new EntitySpawnToken(byte.MinValue);
        public static EntitySpawnToken Max { get; private set; } = new EntitySpawnToken(byte.MaxValue);

        public static bool operator ==(EntitySpawnToken a, EntitySpawnToken b) => a.Equals(b);
        public static bool operator !=(EntitySpawnToken a, EntitySpawnToken b) => !a.Equals(b);

        public static EntitySpawnToken Increment(EntitySpawnToken id)
        {
            var value = id.value;

            value += 1;

            return new EntitySpawnToken(value);
        }
    }
}