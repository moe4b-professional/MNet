using System;
using System.Text;
using System.Collections.Generic;

namespace MNet
{
    public static class BoolFlags
    {
        public static void Populate<T>(T flag, params bool[] values)
            where T : IBoolFlags
        {
            Fill(flag, values);
        }

        public static void Populate<T>(T flag, IList<bool> values)
            where T : IBoolFlags
        {
            Fill(flag, values);
        }

        static void Fill<T>(T flag, IList<bool> values)
            where T : IBoolFlags
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            if (values.Count > flag.Length) throw new IndexOutOfRangeException($"Collection too big ({values.Count}n) to Convert to {typeof(T).Name}");

            for (byte i = 0; i < values.Count; i++)
                flag[i] = values[i];
        }

        public static int CalculateBitRange(byte start, byte end)
        {
            var result = 1;

            for (int i = start; i <= end; i++)
                result *= 2;

            return result;
        }

        public static string ToString<T>(T flag)
            where T : IBoolFlags
        {
            var builder = new StringBuilder();

            builder.Append("(");

            for (byte i = 0; i < flag.Length; i++)
            {
                builder.Append(flag[i]);
                if (i + 1 < flag.Length) builder.Append(", ");
            }

            builder.Append(")");

            return builder.ToString();
        }
    }

    public interface IBoolFlags
    {
        byte Length { get; }

        bool this[byte index] { get; set; }
    }

    public struct Bool8Flags : IBoolFlags, INetworkSerializable
    {
        byte binary;
        public byte Binary => binary;

        public byte Length => Capacity;
        public const byte Capacity = 8;

        public bool this[byte index]
        {
            get
            {
                if (index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                var operand = 1u << index;

                return (binary & operand) == operand;
            }
            set
            {
                if (index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                if (value)
                {
                    var operand = (byte)(1u << index);
                    binary |= operand;
                }
                else
                {
                    var operand = (byte)~(1u << index);
                    binary &= operand;
                }
            }
        }

        public void Select(ref NetworkSerializationContext context) => context.Select(ref binary);

        #region Overrides
        public override int GetHashCode() => binary.GetHashCode();

        public override bool Equals(object obj) => obj is Bool8Flags flag ? CheckEquality(this, flag) : false;

        public static bool CheckEquality(Bool8Flags a, Bool8Flags b) => a.binary == b.binary;

        public override string ToString() => BoolFlags.ToString(this);
        #endregion

        #region Constructors
        public Bool8Flags(byte binary)
        {
            this.binary = binary;
        }
        public Bool8Flags(bool value) : this(value ? byte.MinValue : byte.MaxValue) { }
        #endregion

        #region Operators
        public static bool operator ==(Bool8Flags a, Bool8Flags b) => CheckEquality(a, b);
        public static bool operator !=(Bool8Flags a, Bool8Flags b) => !CheckEquality(a, b);
        #endregion
    }

    public struct Bool16Flags : IBoolFlags, INetworkSerializable
    {
        ushort binary;
        public ushort Binary => binary;

        public byte Length => Capacity;
        public const byte Capacity = 16;

        public bool this[byte index]
        {
            get
            {
                if (index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                var operand = 1u << index;

                return (binary & operand) == operand;
            }
            set
            {
                if (index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                if (value)
                {
                    var operand = (ushort)(1u << index);
                    binary |= operand;
                }
                else
                {
                    var operand = (ushort)~(1u << index);
                    binary &= operand;
                }
            }
        }

        public void Select(ref NetworkSerializationContext context) => context.Select(ref binary);

        #region Overrides
        public override int GetHashCode() => binary.GetHashCode();

        public override bool Equals(object obj) => obj is Bool16Flags flag ? CheckEquality(this, flag) : false;

        public static bool CheckEquality(Bool16Flags a, Bool16Flags b) => a.binary == b.binary;

        public override string ToString() => BoolFlags.ToString(this);
        #endregion

        #region Constructors
        public Bool16Flags(ushort binary)
        {
            this.binary = binary;
        }
        public Bool16Flags(bool value) : this(value ? ushort.MinValue : ushort.MaxValue) { }
        #endregion

        #region Operators
        public static bool operator ==(Bool16Flags a, Bool16Flags b) => CheckEquality(a, b);
        public static bool operator !=(Bool16Flags a, Bool16Flags b) => !CheckEquality(a, b);
        #endregion
    }

    public struct Bool32Flags : IBoolFlags, INetworkSerializable
    {
        uint binary;
        public uint Binary => binary;

        public byte Length => Capacity;
        public const byte Capacity = 32;

        public bool this[byte index]
        {
            get
            {
                if (index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                var operand = 1u << index;

                return (binary & operand) == operand;
            }
            set
            {
                if (index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                if (value)
                {
                    var operand = (1u << index);
                    binary |= operand;
                }
                else
                {
                    var operand = ~(1u << index);
                    binary &= operand;
                }
            }
        }

        public void Select(ref NetworkSerializationContext context) => context.Select(ref binary);

        #region Overrides
        public override int GetHashCode() => binary.GetHashCode();

        public override bool Equals(object obj) => obj is Bool32Flags flag ? CheckEquality(this, flag) : false;

        public static bool CheckEquality(Bool32Flags a, Bool32Flags b) => a.binary == b.binary;

        public override string ToString() => BoolFlags.ToString(this);
        #endregion

        #region Constructors
        public Bool32Flags(uint binary)
        {
            this.binary = binary;
        }
        public Bool32Flags(bool value) : this(value ? uint.MinValue : uint.MaxValue) { }
        #endregion

        #region Operators
        public static bool operator ==(Bool32Flags a, Bool32Flags b) => CheckEquality(a, b);
        public static bool operator !=(Bool32Flags a, Bool32Flags b) => !CheckEquality(a, b);
        #endregion
    }

    public struct Bool64Flags : IBoolFlags, INetworkSerializable
    {
        ulong binary;
        public ulong Binary => binary;

        public byte Length => Capacity;
        public const byte Capacity = 64;

        public bool this[byte index]
        {
            get
            {
                if (index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                var operand = 1ul << index;

                return (binary & operand) == operand;
            }
            set
            {
                if (index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                if (value)
                {
                    var operand = (1ul << index);
                    binary |= operand;
                }
                else
                {
                    var operand = ~(1ul << index);
                    binary &= operand;
                }
            }
        }

        public void Select(ref NetworkSerializationContext context) => context.Select(ref binary);

        #region Overrides
        public override int GetHashCode() => binary.GetHashCode();

        public override bool Equals(object obj) => obj is Bool64Flags flag ? CheckEquality(this, flag) : false;

        public static bool CheckEquality(Bool64Flags a, Bool64Flags b) => a.binary == b.binary;

        public override string ToString() => BoolFlags.ToString(this);
        #endregion

        #region Constructors
        public Bool64Flags(ulong binary)
        {
            this.binary = binary;
        }
        public Bool64Flags(bool value) : this(value ? ulong.MinValue : ulong.MaxValue) { }
        #endregion

        #region Operators
        public static bool operator ==(Bool64Flags a, Bool64Flags b) => CheckEquality(a, b);
        public static bool operator !=(Bool64Flags a, Bool64Flags b) => !CheckEquality(a, b);
        #endregion
    }
}