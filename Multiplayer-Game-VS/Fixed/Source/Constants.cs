using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fixed
{
    public static class Constants
    {
        public static class RestAPI
        {
            public const int Port = 8080;

            public static class Requests
            {
                public const string ListRooms = "/" + nameof(ListRooms);

                public const string CreateRoom = "/" + nameof(CreateRoom);
            }
        }

        public static class WebSockeAPI
        {
            public const int Port = 9090;
        }
    }
}