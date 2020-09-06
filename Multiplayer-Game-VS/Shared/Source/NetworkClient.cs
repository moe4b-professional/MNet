using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    [Serializable]
    public class NetworkClientInfo : INetworkSerializable
    {
        NetworkClientID id;
        public NetworkClientID ID => id;

        NetworkClientProfile profile;
        public NetworkClientProfile Profile => profile;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
            context.Select(ref profile);
        }

        public NetworkClientInfo() { }
        public NetworkClientInfo(NetworkClientID id, NetworkClientProfile profile)
        {
            this.id = id;

            this.profile = profile;
        }
    }

    [Serializable]
    public struct NetworkClientID : INetworkSerializable
    {
        ushort value;
        public ushort Value { get { return value; } }

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref value);
        }

        public NetworkClientID(ushort value)
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

        public static bool operator ==(NetworkClientID a, NetworkClientID b) => a.Equals(b);
        public static bool operator !=(NetworkClientID a, NetworkClientID b) => !a.Equals(b);
    }

    [Serializable]
    public class NetworkClientProfile : INetworkSerializable
    {
        protected string name;
        public string Name { get { return name; } }

        protected AttributesCollection attributes;
        public AttributesCollection Attributes { get { return attributes; } }

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref name);
            context.Select(ref attributes);
        }

        public NetworkClientProfile() { }
        public NetworkClientProfile(string name) : this(name, new AttributesCollection()) { }
        public NetworkClientProfile(string name, AttributesCollection attributes)
        {
            this.name = name;

            this.attributes = attributes;
        }

        public override string ToString() => name;
    }
}