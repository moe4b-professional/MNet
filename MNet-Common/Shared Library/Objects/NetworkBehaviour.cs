using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct NetworkBehaviourID : IEquatable<NetworkBehaviourID>
    {
        byte value;
        public byte Value { get { return value; } }

        public NetworkBehaviourID(byte value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is NetworkBehaviourID target) return Equals(target);

            return false;
        }
        public bool Equals(NetworkBehaviourID id) => this.value == id.value;

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(NetworkBehaviourID a, NetworkBehaviourID b) => a.Equals(b);
        public static bool operator !=(NetworkBehaviourID a, NetworkBehaviourID b) => !a.Equals(b);
    }
}