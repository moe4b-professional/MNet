using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    [Serializable]
    public class ClientInfo : INetSerializable
    {
        protected string name;
        public string Name { get { return name; } }

        protected Dictionary<string,string> attributes;
        public Dictionary<string,string> Attributes { get { return attributes; } }

        public ClientInfo() { }
        public ClientInfo(string name, Dictionary<string, string> attributes)
        {
            this.name = name;

            this.attributes = attributes;
        }
        public ClientInfo(string name) : this(name, null) { }

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

        public override string ToString() => name;
    }
}