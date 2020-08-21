using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net;

using Game.Shared;

namespace Game.Server
{
    static class GameServer
    {
        public static RestAPI Rest { get; private set; }

        public static WebSocketAPI WebSocket { get; private set; }

        public static Lobby Lobby { get; private set; }

        static void Main(string[] args)
        {
            Rest = new RestAPI(IPAddress.Any, Constants.RestAPI.Port);
            Rest.Start();

            WebSocket = new WebSocketAPI(IPAddress.Any, Constants.WebSocketAPI.Port);
            WebSocket.Start();

            Lobby = new Lobby();
            Lobby.Configure();

            //Lobby.CreateRoom("Game Room #1", 4);

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

    public partial class SampleObject : INetworkSerializable
    {
        public int number;

        public string text;

        public string[] array;

        public List<string> list;

        public Dictionary<string, string> dictionary;

        public DateTime date;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(number);
            writer.Write(text);
            writer.Write(array);
            writer.Write(list);
            writer.Write(dictionary);
            writer.Write(date);
        }

        public void Deserialize(NetworkReader reader)
        {
            reader.Read(out number);
            reader.Read(out text);
            reader.Read(out array);
            reader.Read(out list);
            reader.Read(out dictionary);
            reader.Read(out date);
        }

        public SampleObject()
        {

        }

        public static void Test()
        {
            var data = new byte[1024];

            {
                var sample = new SampleObject()
                {
                    number = 42,
                    text = "Hello Serializer",
                    array = new string[]
                    {
                        "Welcome",
                        "To",
                        "Roayal",
                        "Mania"
                    },
                    list = new List<string>()
                    {
                        "The",
                        "Fun",
                        "Is",
                        "Just",
                        "Beginning"
                    },
                    dictionary = new Dictionary<string, string>()
                    {
                        { "Name", "Moe4B" },
                        { "Level", "14" },
                    },
                    date = DateTime.Now,
                };

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                data = NetworkSerializer.Serialize(sample);

                stopwatch.Stop();

                Log.Info("Sample Binary Size: " + data.Length);
                Log.Info("Sample Serialized in: " + stopwatch.ElapsedMilliseconds);
            }

            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var sample = NetworkSerializer.Deserialize<SampleObject>(data);

                stopwatch.Stop();

                Log.Info("Sample Deserialized in: " + stopwatch.ElapsedMilliseconds);

                Log.Info(sample.number);
                Log.Info(sample.text);
                foreach (var item in sample.array) Log.Info(item);
                foreach (var item in sample.list) Log.Info(item);
                foreach (var pair in sample.dictionary) Log.Info(pair);
                Log.Info(sample.date);
            }
        }
    }
}