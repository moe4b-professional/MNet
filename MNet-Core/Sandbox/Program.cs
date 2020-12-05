using System;

using System.Collections;
using System.Collections.Generic;

using System.Diagnostics;

using MNet;

namespace Sandbox
{
    class Program
    {
        static void Main()
        {
            Procedure();

            while (true) Console.ReadKey();
        }

        static void Procedure()
        {

        }

        static void Measure(Action action)
        {
            var watch = Stopwatch.StartNew();

            action();

            watch.Stop();

            Console.WriteLine($"{action.Method.Name} Took: {watch.ElapsedTicks} Ticks, {watch.ElapsedMilliseconds} ms");
        }
    }
}