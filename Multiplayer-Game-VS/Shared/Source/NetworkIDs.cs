using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    [Serializable]
    public struct NetworkClientID : INetworkSerializable
    {
        string value;
        public string Value { get { return value; } }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(value);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out value);
        }

        public NetworkClientID(string value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(NetworkClientID))
            {
                var target = (NetworkClientID)obj;

                return target.value == this.value;
            }

            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static NetworkClientID Empty { get; private set; } = new NetworkClientID(string.Empty);

        public static bool operator ==(NetworkClientID a, NetworkClientID b) => a.Equals(b);
        public static bool operator !=(NetworkClientID a, NetworkClientID b) => !a.Equals(b);

        public static implicit operator NetworkClientID(string value) => new NetworkClientID(value);
        public static implicit operator string(NetworkClientID id) => id.value;
    }

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

        public static NetworkEntityID Empty { get; private set; } = new NetworkEntityID(0);
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