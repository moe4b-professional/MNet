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

    //[Benchmark]
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

    //[Benchmark]
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

    //[Benchmark]
    public void FixedStingSerialization()
    {
        var original = new FixedString32("Hello World");
        var clone = NetworkSerializer.Clone(original);
    }

    [Benchmark]
    public void PrimiriveBlittingSerialization()
    {
        var data = new PrimitiveBlittableData()
        {
            a = 4,
            b = 13,
            c = 12.4f,
            d = 345.6f,
            e = 34,
            f = 230,
        };

        for (int i = 0; i < 1_000; i++)
        {
            NetworkSerializer.Clone(data);
        }
    }

    public struct PrimitiveBlittableData : INetworkSerializable
    {
        public int a, b;
        public float c, d;
        public byte e, f;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref a);
            context.Select(ref b);
            context.Select(ref c);
            context.Select(ref d);
            context.Select(ref e);
            context.Select(ref f);
        }
    }
}