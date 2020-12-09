using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
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