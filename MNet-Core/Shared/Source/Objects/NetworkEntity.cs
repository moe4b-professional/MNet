using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    public enum NetworkEntityType : byte
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
        None = 1 << 0,
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
    public struct NetworkEntityID : INetworkSerializable
    {
        ushort value;
        public ushort Value { get { return value; } }

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref value);
        }

        public NetworkEntityID(ushort value)
        {
            this.value = value;
        }
        NetworkEntityID(int value) : this((ushort)value) { }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(NetworkEntityID))
            {
                var target = (NetworkEntityID)obj;

                return target.value == this.value;
            }

            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(NetworkEntityID a, NetworkEntityID b) => a.Equals(b);
        public static bool operator !=(NetworkEntityID a, NetworkEntityID b) => !a.Equals(b);

        public static NetworkEntityID Increment(NetworkEntityID id) => new NetworkEntityID(id.value + 1);
    }
}