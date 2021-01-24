using System;

using System.IO;
using System.Net;

using System.Threading;
using System.Diagnostics;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace MNet
{
    static class Program
    {
        public const long Count = 10_00_000_000;

        public static void Main(string[] args)
        {
            while (true) Console.ReadKey();
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