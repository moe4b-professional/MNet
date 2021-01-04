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

        public static int data = 42;

        public static void Main(string[] args)
        {
            for (int i = 0; i < 5; i++)
                Measure(Serialization);

            Console.ReadLine();
        }

        static void Serialization()
        {
            var request = RpcRequest.WriteBroadcast(default, default, default, default, 1, 2, 3, 4);

            request.Except(default);

            for (int i = 0; i < Count; i++)
            {
                NetworkSerializer.Serialize(request);
            }
        }

        public static void Measure(Action action)
        {
            var stopwatch = Stopwatch.StartNew();

            action();

            stopwatch.Stop();

            Log.Info($"{action.Method.Name} Toook {stopwatch.Elapsed.TotalMilliseconds.ToString("N")}");
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