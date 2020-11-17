using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

using System.Threading;

using MNet;

namespace StressClient
{
    static class Program
    {
        static WebSocket socket;

        static NetworkClientID clientID;

        static bool isReady = false;

        static NetworkEntityID entityID;

        static void Main()
        {
            var port = Constants.Server.Game.Realtime.Port;

            var room = 0;

            var url = $"ws://127.0.0.1:{port}/{room}";

            socket = new WebSocket(url);

            socket.OnOpen += OnOpen;
            socket.OnMessage += MessageCallback;

            socket.Connect();

            Console.ReadKey();
        }

        static void OnOpen(object sender, EventArgs e)
        {
            {
                var request = new RegisterClientRequest(new NetworkClientProfile("Moe4B"));

                var message = NetworkMessage.Write(request);

                var binary = NetworkSerializer.Serialize(message);

                socket.Send(binary);
            }

            {
                var request = ReadyClientRequest.Write();

                var message = NetworkMessage.Write(request);

                var binary = NetworkSerializer.Serialize(message);

                socket.Send(binary);
            }

            {
                var attributes = new AttributesCollection();

                attributes.Set(0, 0);
                attributes.Set(1, 0);
                attributes.Set(2, 0);

                attributes.Set(3, 0);
                attributes.Set(4, 0);
                attributes.Set(5, 0);

                var request = SpawnEntityRequest.Write("Player", attributes);

                var message = NetworkMessage.Write(request);

                var binary = NetworkSerializer.Serialize(message);

                socket.Send(binary);
            }
        }

        static void MessageCallback(object sender, MessageEventArgs e)
        {
            var binary = e.RawData;

            var message = NetworkMessage.Read(binary);

            Log.Info(message.Type);

            if (message.Type == typeof(RegisterClientResponse))
            {
                var response = message.Read<RegisterClientResponse>();

                isReady = true;

                clientID = response.ID;
            }

            if(isReady)
            {
                if (message.Type == typeof(SpawnEntityCommand))
                {
                    var command = message.Read<SpawnEntityCommand>();

                    if (command.Owner == clientID)
                    {
                        entityID = command.ID;

                        var thread = new Thread(Tick);

                        thread.Start();
                    }
                }
            }
        }

        static void Tick()
        {
            var rpc = RpcRequest.Write(entityID, new NetworkBehaviourID(0), "RpcMove", RpcBufferMode.Last, 0, 0, 0, 0, 0, 0);
            var message = NetworkMessage.Write(rpc);
            var binary = NetworkSerializer.Serialize(message);

            while (true)
            {
                socket.SendAsync(binary, null);

                Thread.Sleep(16);
            }
        }
    }
}