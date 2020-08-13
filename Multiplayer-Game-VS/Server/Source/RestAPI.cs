using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NHttp;

using System.IO;
using System.Net;

namespace Game.Server
{
    class RestAPI
    {
        HttpServer server;

        public void Configure(IPAddress address, int port)
        {
            server = new HttpServer();

            server.EndPoint = new IPEndPoint(address, port);

            server.RequestReceived += RequestCallback;

            Log.Info($"Configuring Rest API on {address}:{port}");
        }

        public void Start()
        {
            Log.Info("Starting Rest API");

            server.Start();
        }

        void RequestCallback(object sender, HttpRequestEventArgs args)
        {
            using (var writer = new StreamWriter(args.Response.OutputStream))
            {
                Log.Info($"Rest API Request: {args.Request.Path} from {args.Request.UserHostAddress}");

                writer.Write("Hello world!");
            }
        }
    }
}
