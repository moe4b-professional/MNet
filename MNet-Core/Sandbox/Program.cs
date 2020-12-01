using System;

using System.Collections;
using System.Collections.Generic;

using System.Diagnostics;

namespace Sandbox
{
    class Program
    {
        static List<string> list = new List<string>();

        static void Main()
        {
            for (int i = 0; i < 10000; i++) list.Add("Hello World");

            Measure(Foreach);
            Measure(Foreach);
            Measure(For);
            Measure(For);

            Console.ReadKey();
        }

        static void Foreach()
        {
            var text = "";

            foreach (var item in list)
            {
                text += item;
            }
        }

        static void For()
        {
            var text = "";

            for (int i = 0; i < list.Count; i++)
            {
                text += list[i];
            }
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