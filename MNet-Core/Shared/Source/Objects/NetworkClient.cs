using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
    public struct NetworkClientID : IManualNetworkSerializable
    {
        byte value;
        public byte Value { get { return value; } }

        public void Serialize(NetworkStream writer)
        {
            writer.Insert(value);
        }

        public void Deserialize(NetworkStream reader)
        {
            value = reader.Pull();
        }

        public NetworkClientID(byte value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is NetworkClientID id) return Equals(id);

            return false;
        }
        public bool Equals(NetworkClientID id) => this.value == id.value;

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(NetworkClientID a, NetworkClientID b) => a.Equals(b);
        public static bool operator !=(NetworkClientID a, NetworkClientID b) => !a.Equals(b);

        public static NetworkClientID Increment(NetworkClientID id)
        {
            var value = id.value;

            value += 1;

            return new NetworkClientID(value);
        }
    }

    [Preserve]
    public class NetworkClientProfile : INetworkSerializable
    {
        string name;
        public string Name
        {
            get => name;
            set => name = value;
        }

        AttributesCollection attributes;
        public AttributesCollection Attributes => attributes;

        public void Select(ref NetworkSerializationContext context)
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

    [Preserve]
    public struct NetworkClientInfo : INetworkSerializable
    {
        NetworkClientID id;
        public NetworkClientID ID => id;

        NetworkClientProfile profile;
        public NetworkClientProfile Profile => profile;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref id);
            context.Select(ref profile);
        }

        public NetworkClientInfo(NetworkClientID id, NetworkClientProfile profile)
        {
            this.id = id;

            this.profile = profile;
        }
    }

    [Preserve]
    public struct NetworkGroupID : IManualNetworkSerializable
    {
        byte value;
        public byte Value { get { return value; } }

        public void Serialize(NetworkStream writer)
        {
            writer.Insert(value);
        }

        public void Deserialize(NetworkStream reader)
        {
            value = reader.Pull();
        }

        public NetworkGroupID(byte value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is NetworkGroupID id) return Equals(id);

            return false;
        }
        public bool Equals(NetworkGroupID id) => this.value == id.value;

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(NetworkGroupID a, NetworkGroupID b) => a.Equals(b);
        public static bool operator !=(NetworkGroupID a, NetworkGroupID b) => !a.Equals(b);

        public static implicit operator NetworkGroupID(byte value) => new NetworkGroupID(value);

        public static NetworkGroupID Create(byte value) => new NetworkGroupID(value);

        public static NetworkGroupID Default { get; private set; } = default;
    }
}