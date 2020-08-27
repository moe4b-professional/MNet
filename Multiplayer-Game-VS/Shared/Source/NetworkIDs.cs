using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    [Serializable]
    public struct NetworkEntityID : INetworkSerializable
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
    }

    [Serializable]
    public struct NetworkBehaviourID : INetworkSerializable
    {
        byte value;
        public byte Value { get { return value; } }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(value);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out value);
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