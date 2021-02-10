using System;
using System.Text;
using System.Collections.Generic;

namespace MNet
{
    public interface IBoolFlags
    {
        int Length { get; }

        bool this[int index] { get; set; }
    }

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

            for (int i = 0; i < values.Count; i++)
                flag[i] = values[i];
        }

        public static string ToString<T>(T flag)
            where T : IBoolFlags
        {
            var builder = new StringBuilder();

            builder.Append("(");

            for (int i = 0; i < flag.Length; i++)
            {
                builder.Append(flag[i]);
                if (i + 1 < flag.Length) builder.Append(", ");
            }

            builder.Append(")");

            return builder.ToString();
        }
    }

    public struct Bool8Flags : IBoolFlags, INetworkSerializable
    {
        byte binary;
        public byte Binary => binary;

        public int Length => Capacity;
        public const int Capacity = 8;

        public bool this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                var operand = 1 << index;

                return (binary & operand) == operand;
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                if (value)
                {
                    var operand = (byte)(1 << index);
                    binary |= operand;
                }
                else
                {
                    var operand = (byte)~(1 << index);
                    binary &= operand;
                }
            }
        }

        public void Select(ref NetworkSerializationContext context) => context.Select(ref binary);

        public override int GetHashCode() => binary.GetHashCode();

        public override bool Equals(object obj) => obj is Bool8Flags flag ? CheckEquality(this, flag) : false;

        public static bool CheckEquality(Bool8Flags a, Bool8Flags b) => a.binary == b.binary;

        public override string ToString() => BoolFlags.ToString(this);

        public Bool8Flags(byte binary)
        {
            this.binary = binary;
        }
        public Bool8Flags(bool value) : this(value ? byte.MinValue : byte.MaxValue) { }

        public static bool operator ==(Bool8Flags a, Bool8Flags b) => CheckEquality(a, b);
        public static bool operator !=(Bool8Flags a, Bool8Flags b) => !CheckEquality(a, b);
    }

    public struct Bool16Flags : IBoolFlags, INetworkSerializable
    {
        short binary;
        public short Binary => binary;

        public int Length => Capacity;
        public const int Capacity = 16;

        public bool this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                var operand = 1 << index;

                return (binary & operand) == operand;
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                if (value)
                {
                    var operand = (short)(1 << index);
                    binary |= operand;
                }
                else
                {
                    var operand = (short)~(1 << index);
                    binary &= operand;
                }
            }
        }

        public void Select(ref NetworkSerializationContext context) => context.Select(ref binary);

        public override int GetHashCode() => binary.GetHashCode();

        public override bool Equals(object obj) => obj is Bool16Flags flag ? CheckEquality(this, flag) : false;

        public static bool CheckEquality(Bool16Flags a, Bool16Flags b) => a.binary == b.binary;

        public override string ToString() => BoolFlags.ToString(this);

        public Bool16Flags(short binary)
        {
            this.binary = binary;
        }
        public Bool16Flags(bool value) : this(value ? short.MinValue : short.MaxValue) { }

        public static Bool16Flags From(params bool[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            if (values.Length > Capacity) throw new IndexOutOfRangeException($"Collection too big to Convert");

            var flag = new Bool16Flags();

            for (int i = 0; i < values.Length; i++)
                flag[i] = values[i];

            return flag;
        }

        public static bool operator ==(Bool16Flags a, Bool16Flags b) => CheckEquality(a, b);
        public static bool operator !=(Bool16Flags a, Bool16Flags b) => !CheckEquality(a, b);
    }

    public struct Bool32Flags : IBoolFlags, INetworkSerializable
    {
        int binary;
        public int Binary => binary;

        public int Length => Capacity;
        public const int Capacity = 32;

        public bool this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                var operand = 1 << index;

                return (binary & operand) == operand;
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                if (value)
                {
                    var operand = (1 << index);
                    binary |= operand;
                }
                else
                {
                    var operand = ~(1 << index);
                    binary &= operand;
                }
            }
        }

        public void Select(ref NetworkSerializationContext context) => context.Select(ref binary);

        public override int GetHashCode() => binary.GetHashCode();

        public override bool Equals(object obj) => obj is Bool32Flags flag ? CheckEquality(this, flag) : false;

        public static bool CheckEquality(Bool32Flags a, Bool32Flags b) => a.binary == b.binary;

        public override string ToString() => BoolFlags.ToString(this);

        public Bool32Flags(int binary)
        {
            this.binary = binary;
        }
        public Bool32Flags(bool value) : this(value ? int.MinValue : int.MaxValue) { }

        public static bool operator ==(Bool32Flags a, Bool32Flags b) => CheckEquality(a, b);
        public static bool operator !=(Bool32Flags a, Bool32Flags b) => !CheckEquality(a, b);
    }

    public struct Bool64Flags : IBoolFlags, INetworkSerializable
    {
        long binary;
        public long Binary => binary;

        public int Length => Capacity;
        public const int Capacity = 64;

        public bool this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                var operand = 1L << index;

                return (binary & operand) == operand;
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                if (value)
                {
                    var operand = (1L << index);
                    binary |= operand;
                }
                else
                {
                    var operand = ~(1L << index);
                    binary &= operand;
                }
            }
        }

        public void Select(ref NetworkSerializationContext context) => context.Select(ref binary);

        public override int GetHashCode() => binary.GetHashCode();

        public override bool Equals(object obj) => obj is Bool64Flags flag ? CheckEquality(this, flag) : false;

        public static bool CheckEquality(Bool64Flags a, Bool64Flags b) => a.binary == b.binary;

        public override string ToString() => BoolFlags.ToString(this);

        public Bool64Flags(long binary)
        {
            this.binary = binary;
        }
        public Bool64Flags(bool value) : this(value ? long.MinValue : long.MaxValue) { }

        public static bool operator ==(Bool64Flags a, Bool64Flags b) => CheckEquality(a, b);
        public static bool operator !=(Bool64Flags a, Bool64Flags b) => !CheckEquality(a, b);
    }

    public abstract class BoolVarFlags : IBoolFlags, IManualNetworkSerializable
    {
        byte[] binary;
        public byte[] Binary => binary;

        public int Length => Segments * Span;

        /// <summary>
        /// Each Segment is 8 Flags
        /// </summary>
        public abstract byte Segments { get; }

        public const byte Span = 8;

        public bool this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                var segment = index / Span;
                var shift = index % Span;

                var operand = 1 << shift;

                return (binary[segment] & operand) == operand;
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index {index} out of Range, Must be Less than {Length}");

                var segment = index / Span;
                var shift = index % Span;

                if (value)
                {
                    var operand = (byte)(1 << shift);
                    binary[segment] |= operand;
                }
                else
                {
                    var operand = (byte)~(1 << shift);
                    binary[segment] &= operand;
                }
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Insert(binary);
        }

        public void Deserialize(NetworkReader reader)
        {
            binary = reader.BlockCopy(Segments);
        }

        public BoolVarFlags()
        {
            binary = new byte[Segments];
        }
    }

    public class Bool128Flags : BoolVarFlags
    {
        public override byte Segments => 128 / Span;
    }
}