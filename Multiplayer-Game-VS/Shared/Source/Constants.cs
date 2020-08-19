using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    public static class Constants
    {
        public static class RestAPI
        {
            public const int Port = 8080;

            public static class Requests
            {
                public static string Path = "/REST";

                public static class Room
                {
                    public static string Path = Requests.Path + "/Room";

                    public static string List { get; private set; } = ToAbsolute(Path, nameof(List));

                    public static string Create { get; private set; } = ToAbsolute(Path, nameof(Create));
                }

                public static string ToAbsolute(string path, string name) => path + "/" + name;
            }
        }

        public static class WebSocketAPI
        {
            public const int Port = 9090;
        }
    }
}