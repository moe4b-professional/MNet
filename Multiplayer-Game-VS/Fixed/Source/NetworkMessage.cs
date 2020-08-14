using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;

namespace Game.Fixed
{
    [Serializable]
    public class NetworkMessage
    {
        public void WriteTo(HttpListenerResponse response)
        {
            response.StatusCode = (int)HttpStatusCode.OK;

            var data = NetworkSerializer.Serialize(this);

            response.WriteContent(data);

            response.Close();
        }

        public static byte[] Serialize(NetworkMessage message)
        {
            var data = NetworkSerializer.Serialize(message);

            return data;
        }

        public static T Deserialize<T>(byte[] data)
            where T : NetworkMessage
        {
            var target = NetworkSerializer.Deserialize(data) as T;

            return target;
        }
    }

    [Serializable]
    public class ListRoomsMessage : NetworkMessage
    {
        public IList<RoomInfo> list { get; protected set; }

        public ListRoomsMessage(IList<RoomInfo> list)
        {
            this.list = list;
        }
    }
}