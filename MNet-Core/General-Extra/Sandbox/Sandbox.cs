using System;
using System.Net;

namespace MNet
{
    public class Sandbox
    {
        public static unsafe void Execute()
        {
            var address4 = ValueIPAddress.Parse("127.0.0.1");
            Log.Info(address4);

            var addres6 = ValueIPAddress.Parse("0123:4567:89AB:CDEF:0000:FFFF:0000:FFFF");
            Log.Info(addres6);

            Log.Info(CompareIPs("0123:4567:89AB:CDEF:0000:FFFF:0000:FFFF"));
            Log.Info(CompareIPs("104.220.80.144"));

            Console.ReadKey();
        }

        static bool CompareIPs(ReadOnlySpan<char> characters)
        {
            var original = IPAddress.Parse(characters);
            var clone = ValueIPAddress.Parse(characters);

            return CompareIPs(original, clone);
        }
        static bool CompareIPs(ReadOnlySpan<byte> binary)
        {
            var original = new IPAddress(binary);
            var clone = ValueIPAddress.Create(binary);

            return CompareIPs(original, clone);
        }

        static bool CompareIPs(IPAddress address1, ValueIPAddress address2)
        {
            Span<byte> span1 = stackalloc byte[16];

            if (address1.TryWriteBytes(span1, out var length) == false)
                throw new Exception();

            span1 = span1.Slice(0, length);

            var span2 = address2.ToSpan();

            return MemoryExtensions.SequenceEqual(span1, span2);
        }
    }
}