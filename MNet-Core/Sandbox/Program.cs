using System;
using System.Diagnostics;

namespace MNet
{
    class Program
    {
        public const int Count = 10_000_000;

        public static int data = 42;

        static void Main(string[] args)
        {
            NetworkSerializer.Clone(data);

            Measure(NormalSerialize);
            Measure(ResolveDeserialize);
            Measure(ResolvedSerialize);

            while (true) Console.ReadKey();
        }

        static void CheckNull()
        {
            for (int i = 0; i < Count * 100; i++)
            {
                var result = NetworkSerializationHelper.Nullable.Generic<int>.Is;
            }
        }

        static void NormalSerialize()
        {
            for (int i = 0; i < Count; i++)
            {
                var result = BitConverter.GetBytes(Count);
            }
        }

        static void ResolveDeserialize()
        {
            var binary = new byte[] { 42, 0, 0, 0 };

            for (int i = 0; i < Count; i++)
                NetworkSerializer.Deserialize<int>(binary);
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