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
        public const long Count = 10_00_000_000;

        public static void Main(string[] args)
        {
            for (long i = 0; i < 50; i++)
            {
                PerformanceTools.Measure(StructTest);
            }

            Console.Read();

            for (long i = 0; i < 50; i++)
            {
                PerformanceTools.Measure(StructTest);
                PerformanceTools.Measure(ClassTest);

                Console.WriteLine();
            }

            Console.ReadLine();
        }

        static void StructTest()
        {
            for (long i = 0; i < Count; i++)
            {
                var sample = new StructSample(42, 420f);

                PerformanceTools.Consume(sample.a);
                PerformanceTools.Consume(sample.b);
            }
        }
        struct StructSample
        {
            public int a { get; private set; }

            public float b { get; private set; }

            public StructSample(int a, float b)
            {
                this.a = a;
                this.b = b;
            }
        }

        static void ClassTest()
        {
            for (long i = 0; i < Count; i++)
            {
                var sample = new ClassSample(42, 420f);

                PerformanceTools.Consume(sample.a);
                PerformanceTools.Consume(sample.b);
            }
        }
        class ClassSample
        {
            public int a { get; private set; }

            public float b { get; private set; }

            public ClassSample(int a, float b)
            {
                this.a = a;
                this.b = b;
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