using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Game.Fixed
{
    public static class NetworkSerializer
    {
        public static BinaryFormatter Formatter { get; private set; }

        public static byte[] Serialize(object target)
        {
            using (var stream = new MemoryStream())
            {
                Formatter.Serialize(stream, target);

                return stream.ToArray();
            }
        }

        public static object Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                var target = Formatter.Deserialize(stream);

                return target;
            }
        }

        static NetworkSerializer()
        {
            Formatter = new BinaryFormatter();
        }
    }
}