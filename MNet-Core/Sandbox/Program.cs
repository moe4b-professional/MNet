using System;
using System.Diagnostics;

using ProtoBuf;
using System.IO;

using System.Collections.Generic;

namespace MNet
{
    static class Program
    {
        public const int Count = 1_000_000;

        public static void Main(string[] args)
        {
            while (true) Console.ReadKey();
        }

        static void Measure(Action action)
        {
            var stopwatch = Stopwatch.StartNew();

            action();

            stopwatch.Stop();

            Log.Info($"{action.Method.Name} Toook {stopwatch.Elapsed.TotalMilliseconds.ToString("N")}");
        }
    }

    static class OLD
    {
        public const int Count = 10_000_000;

        public static int data = 42;

        static void ADS(string[] args)
        {
            DynamicNetworkSerialization.Enabled = true;

            Prepare();

            Measure(SerializeRPC);
            Measure(ResolveDeserialize);
            Measure(ResolvedSerialize);

            while (true) Console.ReadKey();
        }

        static void Prepare()
        {
            NetworkSerializer.Clone(data);

            var request = RpcRequest.Write(default, default, new RpcMethodID("Method"), default, 42, 42);
            var command = RpcCommand.Write(default, request, default);
            NetworkSerializer.Serialize(command);
        }

        static void SerializeRPC()
        {
            var request = RpcRequest.Write(default, default, new RpcMethodID("Method"), default, 42, 42);

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

        static void Measure(Action action)
        {
            var stopwatch = Stopwatch.StartNew();

            action();

            stopwatch.Stop();

            Log.Info($"{action.Method.Name} Toook {stopwatch.ElapsedMilliseconds.ToString("N")}");
        }
    }
}