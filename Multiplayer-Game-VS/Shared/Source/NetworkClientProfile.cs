using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    [Serializable]
    public class NetworkClientProfile : INetworkSerializable
    {
        protected string name;
        public string Name { get { return name; } }

        protected AttributesCollection attributes;
        public AttributesCollection Attributes { get { return attributes; } }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(name);
            writer.Write(attributes);
        }
        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out name);
            reader.Read(out attributes);
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