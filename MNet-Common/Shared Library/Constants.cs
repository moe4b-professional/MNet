﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    public static class Constants
    {
        public const string Name = "MNet";

        public const string Path = Name + "/";

        public static Version ApiVersion { get; private set; } = new Version(0, 0, 17);

        public const int IdRecycleLifeTime = 60;

        public static class Server
        {
            public static class Master
            {
                public static class Rest
                {
                    public const int Port = 7070;

                    public static class Requests
                    {
                        public static string Path { get; private set; } = "/";

                        public static string Scheme { get; private set; } = Path + nameof(Scheme);

                        public static string Info { get; private set; } = Path + nameof(Info);

                        public static class Server
                        {
                            public static string Path { get; private set; } = Requests.Path + $"{nameof(Server)}/";

                            public static string Register { get; private set; } = Path + nameof(Register);

                            public static string Unregister { get; private set; } = Path + nameof(Unregister);
                        }
                    }
                }
            }

            public static class Game
            {
                public static class Rest
                {
                    public const int Port = 8080;

                    public static class Requests
                    {
                        static readonly string Path = "/";

                        public static string Info { get; private set; } = Path + nameof(Info);

                        public static class Lobby
                        {
                            static readonly string Path = Requests.Path + $"{nameof(Lobby)}/";

                            public static string Info { get; private set; } = Path + nameof(Info);
                        }

                        public static class Room
                        {
                            static readonly string Path = Requests.Path + $"{nameof(Room)}/";

                            public static string Create { get; private set; } = Path + nameof(Create);
                        }
                    }
                }

                public static class Realtime
                {

                }
            }
        }
    }
}