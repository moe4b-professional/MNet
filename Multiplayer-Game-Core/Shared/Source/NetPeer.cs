using System;
using System.Text;
using System.Collections.Generic;

namespace Backend
{
    public struct NetPeerID
    {
        ushort value;
        public ushort Value { get { return value; } }

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref value);
        }

        public NetPeerID(ushort value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(NetPeerID))
            {
                var target = (NetPeerID)obj;

                return target.value == this.value;
            }

            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(NetPeerID a, NetPeerID b) => a.Equals(b);
        public static bool operator !=(NetPeerID a, NetPeerID b) => !a.Equals(b);
    }
}