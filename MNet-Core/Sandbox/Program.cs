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
        public static void Main(string[] args)
        {
            Procedure();

            while (true) Console.ReadKey();
        }

        static void Procedure()
        {

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