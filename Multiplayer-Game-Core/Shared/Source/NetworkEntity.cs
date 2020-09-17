using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    public enum NetworkEntityType : byte
    {
        Dynamic, SceneObject
    }

    [Serializable]
    public struct NetworkEntityID : INetworkSerializable
    {
        ushort value;
        public ushort Value { get { return value; } }

        public void Select(INetworkSerializableResolver.Context context)
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

    [Serializable]
    public struct NetworkBehaviourID : INetworkSerializable
    {
        byte value;
        public byte Value { get { return value; } }

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref value);
        }

        public NetworkBehaviourID(byte value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(NetworkBehaviourID))
            {
                var target = (NetworkBehaviourID)obj;

                return target.value == this.value;
            }

            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(NetworkBehaviourID a, NetworkBehaviourID b) => a.Equals(b);
        public static bool operator !=(NetworkBehaviourID a, NetworkBehaviourID b) => !a.Equals(b);
    }
}