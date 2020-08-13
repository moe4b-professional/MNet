using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;

namespace Game.Server
{
    class Program
    {
        static RestAPI rest;

        static WebSockeAPI websocket;

        static void Main(string[] args)
        {
            rest = new RestAPI();
            rest.Configure(IPAddress.Any, Constants.RestAPI.Port);
            rest.Start();

            websocket = new WebSockeAPI();
            websocket.Configure(IPAddress.Any, Constants.WebSockeAPI.Port);
            websocket.Start();

            Console.ReadKey();
        }
    }
}
