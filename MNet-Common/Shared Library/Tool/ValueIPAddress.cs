using System;
using System.Globalization;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace MNet
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ValueIPAddress : IEquatable<ValueIPAddress>
    {
        const int MaxSize = 16;

        fixed byte binary[MaxSize];

        public Family Type { get; }
        public enum Family : byte
        {
            v4 = 4,
            v6 = 16
        }

        public ref byte GetPinnableReference() => ref binary[0];

        #region Span Operations
        public ReadOnlySpan<byte> ToSpan()
        {
            fixed (byte* ptr = binary)
            {
                var span = new Span<byte>(ptr, (int)Type);
                return span;
            }
        }

        public ReadOnlySpan<char> ToCharacters(Span<char> buffer)
        {
            var builder = new SpanStringBuilder(buffer);

            switch (Type)
            {
                case Family.v4:
                {
                    builder.Append(binary[0]);
                    builder.Append('.');
                    builder.Append(binary[1]);
                    builder.Append('.');
                    builder.Append(binary[2]);
                    builder.Append('.');
                    builder.Append(binary[3]);
                }
                break;

                case Family.v6:
                {
                    fixed (void* ptr = binary)
                    {
                        var ocelots = new Span<ushort>(ptr, 8);

                        for (int i = 0; i < ocelots.Length; i++)
                        {
                            var length = HexUtility.Write(ocelots[i], builder.Buffer());

                            builder.Increment(length);

                            if (i + 1 < ocelots.Length)
                                builder.Append(':');
                        }
                    }
                }
                break;

                default:
                    throw new NotImplementedException();
            }

            return builder.ToSpan();
        }
        #endregion

        #region Overrides
        public override int GetHashCode()
        {
            fixed (byte* ptr = binary)
            {
                switch (Type)
                {
                    case Family.v4:
                    {
                        return *(int*)ptr;
                    }

                    case Family.v6:
                    {
                        var root = (long*)ptr;
                        return root[1].GetHashCode() ^ root[2].GetHashCode();
                    }

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public override string ToString()
        {
            Span<char> buffer = stackalloc char[GetMaxCharacterBufferSize(Type)];

            var slice = ToCharacters(buffer);

            return new string(slice);
        }
        #endregion

        #region Equals
        public override bool Equals(object obj)
        {
            if (obj is ValueIPAddress target)
                return Equals(target);

            return false;
        }

        public bool Equals(ValueIPAddress target)
        {
            if (this.Type != target.Type)
                return false;

            for (int i = 0; i < (int)Type; i++)
                if (this.binary[i] != target.binary[i])
                    return false;

            return true;
        }
        #endregion

        private ValueIPAddress(ReadOnlySpan<byte> span, Family type)
        {
            if (span.Length > MaxSize)
                throw new ArgumentException($"Span too Big, Max Size is {MaxSize}");

            this.Type = type;

            fixed (byte* ptr = binary)
            {
                var destination = new Span<byte>(ptr, MaxSize);

                span.CopyTo(destination);
            }
        }

        //Static Utiltiy

        public static ValueIPAddress Create(ReadOnlySpan<byte> span)
        {
            switch (span.Length)
            {
                case (int)Family.v4:
                    return new ValueIPAddress(span, Family.v4);

                case (int)Family.v6:
                    return new ValueIPAddress(span, Family.v6);

                default:
                    throw new ArgumentException($"Invalid Span Size");
            }
        }

        #region Parse
        public static ValueIPAddress Parse(ReadOnlySpan<char> characters)
        {
            if (TryParse(characters, out var address) == false)
                throw new ArgumentException();

            return address;
        }

        public static bool TryParse(ReadOnlySpan<char> characters, out ValueIPAddress address)
        {
            if (TryDetermineType(characters, out var type) == false)
            {
                address = default;
                return false;
            }

            switch (type)
            {
                case Family.v4:
                    return TryParseIPv4(characters, out address);

                case Family.v6:
                    return TryParseIPv6(characters, out address);

                default:
                    throw new NotImplementedException();
            }
        }

        private static bool TryParseIPv4(ReadOnlySpan<char> characters, out ValueIPAddress address)
        {
            var ocelots = stackalloc byte[4];

            var index = 0;
            var start = 0;

            for (int i = 0; i <= characters.Length; i++)
            {
                if (i == characters.Length || characters[i] == '.')
                {
                    if (index == 4)
                    {
                        address = default;
                        return false;
                    }

                    var split = characters.Slice(start, i - start);
                    if (byte.TryParse(split, out ocelots[index]) == false)
                    {
                        address = default;
                        return false;
                    }

                    index += 1;
                    start = i + 1;
                }
            }

            var span = new Span<byte>(ocelots, 4);
            address = new ValueIPAddress(span, Family.v4);
            return true;
        }
        private static bool TryParseIPv6(ReadOnlySpan<char> characters, out ValueIPAddress address)
        {
            var ocelots = stackalloc ushort[8];

            var index = 0;
            var start = 0;

            for (int i = 0; i <= characters.Length; i++)
            {
                if (i == characters.Length || characters[i] == ':')
                {
                    if (index == 8)
                    {
                        address = default;
                        return false;
                    }

                    var split = characters.Slice(start, i - start);
                    if (HexUtility.TryParse(split, out ocelots[index]) == false)
                    {
                        address = default;
                        return false;
                    }

                    index += 1;
                    start = i + 1;
                }
            }

            var span = new Span<byte>(ocelots, 16);
            address = new ValueIPAddress(span, Family.v6);
            return true;
        }

        private static bool TryDetermineType(ReadOnlySpan<char> characters, out Family type)
        {
            for (int i = 0; i < 5; i++)
            {
                if (characters[i] == '.')
                {
                    type = Family.v4;
                    return true;
                }

                if (characters[i] == ':')
                {
                    type = Family.v6;
                    return true;
                }
            }

            type = default;
            return false;
        }
        #endregion

        public static int GetMaxCharacterBufferSize(Family type)
        {
            switch (type)
            {
                case Family.v4:
                    return 15;

                case Family.v6:
                    return 39;

                default:
                    throw new NotImplementedException();
            }
        }

        public static class HexUtility
        {
            public static int Write(ushort value, Span<char> buffer)
            {
                if (value == 0)
                    return default;

                if (BitConverter.IsLittleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);

                if (value.TryFormat(buffer, out var length, "X") == false)
                    throw new ArgumentException();

                return length;
            }

            public static bool TryParse(ReadOnlySpan<char> characters, out ushort value)
            {
                if (characters.Length == 0)
                {
                    value = 0;
                    return true;
                }

                if (ushort.TryParse(characters, NumberStyles.HexNumber, default, out value) == false)
                {
                    value = default;
                    return false;
                }

                if (BitConverter.IsLittleEndian)
                    value = BinaryPrimitives.ReverseEndianness(value);

                return true;
            }
        }

        #region Equality Operator
        public static bool operator ==(ValueIPAddress left, ValueIPAddress right) => left.Equals(right);

        public static bool operator !=(ValueIPAddress left, ValueIPAddress right) => !left.Equals(right);
        #endregion
    }
}