using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net;

using Game.Fixed;

namespace Game.Server
{
    static class GameServer
    {
        public static RestAPI Rest { get; private set; }

        public static WebSocketAPI WebSocket { get; private set; }

        public static Lobby Lobby { get; private set; }

        static void Main(string[] args)
        {
            NetworkMessagePayload.ValidateAll();

            Rest = new RestAPI(IPAddress.Any, Constants.RestAPI.Port);
            Rest.Start();

            WebSocket = new WebSocketAPI(IPAddress.Any, Constants.WebSocketAPI.Port);
            WebSocket.Start();

            Lobby = new Lobby();
            Lobby.Configure();

            Lobby.CreateRoom("Game Room #1");

            Sandbox.Run();

            Console.ReadKey();
        }
    }

    public static class Sandbox
    {
        public static void Run()
        {
            
        }
    }
}