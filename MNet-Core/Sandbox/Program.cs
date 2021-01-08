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
        public const int Count = 1_000_000;

        public static void Main(string[] args)
        {
            DynamicNetworkSerialization.Enabled = true;

            Setup();

            for (int i = 0; i < 20; i++)
                PerformanceTools.Measure(Insert);

            Console.ReadLine();
        }

        static void Insert()
        {
            var raw = new byte[256];

            for (int i = 0; i < raw.Length; i++)
                raw[i] = (byte)i;

            var writer = new NetworkWriter(512);

            for (int i = 0; i < Count; i++)
            {
                writer.Insert(raw);
                PerformanceTools.Consume(writer.Position);
                writer.Clear();
            }
        }

        static void Serialization()
        {
            var request = RpcRequest.WriteBroadcast(default, default, default, default, 1, 2, 3, 4);

            for (int i = 0; i < Count; i++)
            {
                NetworkSerializer.Serialize(request);
            }
        }

        static void Setup()
        {
            var request = RpcRequest.WriteBroadcast(default, default, default, default, 1, 2, 3, 4);

            for (int i = 0; i < 100; i++)
            {
                NetworkSerializer.Serialize(request);
            }
        }
    }

    static class PerformanceTools
    {
        public static void Measure(Action action) => Measure(action, action.Method.Name);
        public static void Measure(Action action, string name)
        {
            var stopwatch = Stopwatch.StartNew();

            action();

            stopwatch.Stop();

            Log.Info($"{action.Method.Name} Took {stopwatch.Elapsed.TotalMilliseconds.ToString("N")}");
        }

        public static void Consume<T>(T value) { }
    }
}