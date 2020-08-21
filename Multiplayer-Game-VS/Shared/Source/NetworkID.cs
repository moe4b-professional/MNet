using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    public struct NetworkID : INetSerializable
    {
        private Guid value;
        public Guid Value { get { return value; } }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(value);
        }

        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out value);
        }

        public NetworkID(Guid value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(NetworkID))
            {
                var target = (NetworkID)obj;

                return target.value == this.value;
            }

            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(NetworkID a, NetworkID b) => a.Equals(b);
        public static bool operator !=(NetworkID a, NetworkID b) => !a.Equals(b);

        public static NetworkID Empty { get; private set; } = new NetworkID(Guid.Empty);
    }

    public struct NetworkClientID
    {
        public Guid Value { get; private set; }

        public NetworkClientID(Guid value)
        {
            this.Value = value;
        }
    }

    public struct NetworkEntityID
    {
        public Guid Value { get; private set; }
    }
}