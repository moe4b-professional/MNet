using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fixed
{
    [Serializable]
    public class BaseNetworkMessage
    {
        public static byte[] Serialize(BaseNetworkMessage message)
        {
            var data = NetworkSerializer.Serialize(message);

            return data;
        }

        public static T Deserialize<T>(byte[] data)
            where T : BaseNetworkMessage
        {
            var target = NetworkSerializer.Deserialize(data) as T;

            return target;
        }
    }

    [Serializable]
    public class NetworkMessage<T> : BaseNetworkMessage
        where T : BaseNetworkMessage
    {
        public static T Deserialize(byte[] data) => Deserialize<T>(data);
    }

    [Serializable]
    public class ListRoomsMessage : NetworkMessage<ListRoomsMessage>
    {
        public IList<RoomInfo> list { get; protected set; }

        public ListRoomsMessage(IList<RoomInfo> list)
        {
            this.list = list;
        }
    }
}