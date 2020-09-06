using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net;

namespace Backend
{
    static class GameServer
    {
        public static RestAPI Rest { get; private set; }

        public static WebSocketAPI WebSocket { get; private set; }

        public static Lobby Lobby { get; private set; }

        static void Main(string[] args)
        {
            var address = IPAddress.Any;

            Rest = new RestAPI(address, Constants.RestAPI.Port);
            Rest.Start();

            WebSocket = new WebSocketAPI(address, Constants.WebSocketAPI.Port);
            WebSocket.Start();

            Lobby = new Lobby();
            Lobby.Configure();

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

    public class SerializationSample : INetworkSerializable
    {
        public int number;

        public string text;

        public string[] array;

        public List<string> list;

        public Dictionary<string, string> dictionary;

        public DateTime date;

        public Dictionary<string, string> attribute;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref number);
            context.Select(ref text);
            context.Select(ref array);
            context.Select(ref list);
            context.Select(ref dictionary);
            context.Select(ref date);
            context.Select(ref attribute);
        }

        public SerializationSample()
        {

        }

        public static void Test()
        {
            byte[] data;

            {
                var sample = new SerializationSample()
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
                    attribute = new Dictionary<string, string>()
                    {
                        { "Level", "4" }
                    }
                };

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                data = NetworkSerializer.Serialize(sample);

                stopwatch.Stop();

                Log.Info("Sample Binary Size: " + data.Length);
                Log.Info("Sample Serialized in: " + stopwatch.ElapsedMilliseconds);
            }

            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var sample = NetworkSerializer.Deserialize<SerializationSample>(data);

                stopwatch.Stop();

                Log.Info("Sample Deserialized in: " + stopwatch.ElapsedMilliseconds);

                Log.Info(sample.number);
                Log.Info(sample.text);
                foreach (var item in sample.array) Log.Info(item);
                foreach (var item in sample.list) Log.Info(item);
                foreach (var pair in sample.dictionary) Log.Info(pair);
                Log.Info(sample.date);
                foreach (var pair in sample.attribute) Log.Info(pair);
            }
        }
    }
}