using System;
using System.Diagnostics;

using System.IO;

using System.Collections.Generic;

using System.Threading;

using System.Net;

namespace MNet
{
    static class Program
    {
        public const int Count = 10_000_000;

        public static int data = 42;

        public static void Main(string[] args)
        {
            DynamicNetworkSerialization.Enabled = true;

            HashSet<C> queue = new HashSet<C>();

            for (int i = 0; i < Count; i++)
                queue.Add(new C());

            var c = new C();

            for (int i = 0; i < 20; i++)
            {
                queue.Contains(c);
            }

            return;
            ThreadMan.Run();
            
            Log.Info("Complete");

            while (true) Console.ReadKey();
        }

        class C
        {

        }

        public static void Measure(Action action)
        {
            var stopwatch = Stopwatch.StartNew();

            action();

            stopwatch.Stop();

            Log.Info($"{action.Method.Name} Toook {stopwatch.Elapsed.TotalMilliseconds.ToString("N")}");
        }
    }

    public static class ThreadMan
    {
        static LobbyInfo info;

        public static void Run()
        {
            var server = new GameServerID(IPAddress.Parse("10.4.4.2"));

            var attributes = new AttributesCollection();

            attributes.Set(0, "Level 1");

            var rooms = new List<RoomBasicInfo>()
            {
                new RoomBasicInfo(new RoomID(0), "Game Room #1", 10, 5, attributes),
                new RoomBasicInfo(new RoomID(1), "Game Room #2", 10, 5, attributes),
            };

            info = new LobbyInfo(server, rooms);

            for (int i = 0; i < 40; i++) new Thread(Test).Start();
        }

        static void Test()
        {
            for (int i = 0; i < 20_000; i++)
            {
                try
                {
                    var clone = NetworkSerializer.Clone(info);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            Log.Info("Complete");
        }
    }

    /*
    public static class Serialization
    {
        public const int Count = 10_000_000;

        public static int data = 42;

        static void Start()
        {
            Prepare();

            Program.Measure(ResolveDeserialize);
            Program.Measure(ResolvedSerialize);
        }

        static void Prepare()
        {
            NetworkSerializer.Clone(data);

            var request = RpcRequest.Write(default, default, new RpcMethodID(0), default, 42, 42);
            var command = RpcCommand.Write(default, request, default);
            NetworkSerializer.Serialize(command);
        }

        static void SerializeRPC()
        {
            var request = RpcRequest.Write(default, default, new RpcMethodID(0), default, 42, 42);

            var command = RpcCommand.Write(default, request, default);

            for (int i = 0; i < 8000; i++) NetworkSerializer.Serialize(command);
        }

        static void ResolveDeserialize()
        {
            var binary = new byte[] { 42, 0, 0, 0 };

            for (int i = 0; i < Count; i++) NetworkSerializer.Deserialize<int>(binary);
        }

        static void ResolvedSerialize()
        {
            for (int i = 0; i < Count; i++) NetworkSerializer.Serialize(data);
        }
    }
    */
}