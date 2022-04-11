using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using MNet;

using System.Runtime.InteropServices;

BenchmarkRunner.Run<Benchmark>();

[MemoryDiagnoser]
public class Benchmark
{
    int iterations = 100_000;

    [GlobalSetup]
    public void Setup()
    {
        BlittableSerialization();
        DefaultSerialization();
    }

    [Benchmark]
    public void BlittableSerialization()
    {
        for (int i = 1; i <= iterations; i++)
        {
            var data = new BlittableData()
            {
                x = i,
                y = i / 2,
                z = i / 3,
                w = i / 4,
            };

            NetworkSerializer.Clone(data);
        }
    }
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct BlittableData
    {
        public int x, y, z, w;
    }

    [Benchmark]
    public void DefaultSerialization()
    {
        for (int i = 1; i <= iterations; i++)
        {
            var data = new INetworkData()
            {
                x = i,
                y = i / 2,
                z = i / 3,
                w = i / 4,
            };

            NetworkSerializer.Clone(data);
        }
    }
    public struct INetworkData : INetworkSerializable
    {
        public int x, y, z, w;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref x);
            context.Select(ref y);
            context.Select(ref z);
            context.Select(ref w);
        }
    }
}