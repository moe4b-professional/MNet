using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MNet
{
    public static unsafe class FixedString
    {
        public const int MaxSize = 128;

        #region Matching & Comparison
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Matches<TLeft, TRight>(this TLeft left, TRight right, StringComparison comparison = StringComparison.Ordinal)
            where TLeft : IFixedString
            where TRight : IFixedString
        {
            if (left.Length != right.Length)
                return false;

            var leftSpan = left.ToSpan();
            var rightSpan = right.ToSpan();

            return MemoryExtensions.Equals(leftSpan, rightSpan, comparison);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Matches<TLeft>(this TLeft left, ReadOnlySpan<char> right, StringComparison comparison = StringComparison.Ordinal)
            where TLeft : IFixedString
        {
            if (left.Length != right.Length)
                return false;

            var leftSpan = left.ToSpan();

            return MemoryExtensions.Equals(leftSpan, right, comparison);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CompareTo<TLeft, TRight>(this TRight left, TLeft right, StringComparison comparison = StringComparison.Ordinal)
            where TLeft : IFixedString
            where TRight : IFixedString
        {
            var leftSpan = left.ToSpan();
            var rightSpan = right.ToSpan();

            return MemoryExtensions.CompareTo(leftSpan, rightSpan, comparison);
        }
        #endregion

        #region Overrides
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode<T>(ref T target)
            where T : struct, IFixedString
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                fixed (char* ptr = target)
                {
                    int i = 0;

                    while(true)
                    {
                        hash1 = ((hash1 << 5) + hash1) ^ ptr[i];

                        if (i >= target.Length)
                            break;

                        hash2 = ((hash2 << 5) + hash2) ^ ptr[i + 1];

                        i += 2;
                    }
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToString<T>(ref T target)
            where T : struct, IFixedString
        {
            var span = target.ToSpan();
            return new string(span);
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Clone<T>(this T target)
            where T : struct, IFixedString
        {
            return target;
        }

        public struct Numerator<T>
            where T : IFixedString
        {
            T Text;
            int index;

            public char Current => Text[index];

            public bool MoveNext()
            {
                index += 1;

                if (index >= Text.Length)
                    return false;

                return true;
            }

            public Numerator(ref T text)
            {
                this.Text = text;
                index = -1;
            }
        }
    }

    public interface IFixedString
    {
        int Length { get; set; }

        int Capacity { get; }

        char this[int index] { get; set; }

        public Span<char> ToSpan();

        ref char GetPinnableReference();
    }

    #region Variants
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FixedString16 : IFixedString, IEquatable<FixedString16>, IComparable<FixedString16>
    {
        public const int MaxSize = 16;

        fixed char buffer[MaxSize];

        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index of {index} out of Length of {Length}");

                return buffer[index];
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index of {index} out of Length of {Length}");

                buffer[index] = value;
            }
        }

        int internal_length;
        public int Length
        {
            get => internal_length;
            set
            {
                if (internal_length > MaxSize || internal_length < 0)
                    throw new ArgumentOutOfRangeException(nameof(Length));

                internal_length = value;
            }
        }

        public int Capacity => MaxSize;

        public Span<char> ToSpan()
        {
            fixed (char* pointer = buffer)
            {
                return new Span<char>(pointer, Length);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref char GetPinnableReference() => ref buffer[0];

        public FixedString.Numerator<FixedString16> GetEnumerator() => new FixedString.Numerator<FixedString16>(ref this);

        #region Object Overrides
        public override int GetHashCode() => FixedString.GetHashCode(ref this);
        public override string ToString() => FixedString.ToString(ref this);

        public override bool Equals(object obj)
        {
            if (obj is IFixedString target)
                return FixedString.Matches(this, target);

            return false;
        }
        #endregion

        #region Equality & Comparison Methods
        public bool Equals(FixedString16 target) => FixedString.Matches(this, target);

        public int CompareTo(FixedString16 target) => FixedString.CompareTo(this, target);
        #endregion

        #region Constructors
        public FixedString16(ReadOnlySpan<char> source)
        {
            if (source.Length > MaxSize)
                throw new InvalidOperationException($"Source Bigger than Max Size of {MaxSize}");

            internal_length = source.Length;

            fixed (char* ptr = buffer)
            {
                var destination = new Span<char>(ptr, internal_length);

                source.CopyTo(destination);
            }
        }

        public FixedString16(int length)
        {
            if (length > MaxSize)
                throw new InvalidOperationException($"Argument Length Bigger than Max Size of {MaxSize}");

            internal_length = length;
        }

        public FixedString16(bool fill)
        {
            if (fill)
                internal_length = MaxSize;
            else
                internal_length = 0;
        }
        #endregion

        #region Equality Operators
        public static bool operator ==(FixedString16 left, FixedString16 right) => FixedString.Matches(left, right);
        public static bool operator !=(FixedString16 left, FixedString16 right) => !FixedString.Matches(left, right);

        public static bool operator ==(FixedString16 left, string right) => FixedString.Matches(left, right);
        public static bool operator !=(FixedString16 left, string right) => !FixedString.Matches(left, right);

        public static bool operator ==(string right, FixedString16 left) => FixedString.Matches(left, right);
        public static bool operator !=(string right, FixedString16 left) => !FixedString.Matches(left, right);
        #endregion

        #region Conversions Operators
        public static implicit operator FixedString16(ReadOnlySpan<char> span) => new FixedString16(span);
        public static implicit operator ReadOnlySpan<char>(FixedString16 span) => span.ToSpan();

        public static implicit operator Span<char>(FixedString16 span) => span.ToSpan();

        public static implicit operator FixedString16(string text) => new FixedString16(text);
        #endregion
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FixedString32 : IFixedString, IEquatable<FixedString32>, IComparable<FixedString32>
    {
        public const int MaxSize = 32;

        fixed char buffer[MaxSize];

        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index of {index} out of Length of {Length}");

                return buffer[index];
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index of {index} out of Length of {Length}");

                buffer[index] = value;
            }
        }

        int internal_length;
        public int Length
        {
            get => internal_length;
            set
            {
                if (internal_length > MaxSize || internal_length < 0)
                    throw new ArgumentOutOfRangeException(nameof(Length));

                internal_length = value;
            }
        }

        public int Capacity => MaxSize;

        public Span<char> ToSpan()
        {
            fixed (char* pointer = buffer)
            {
                return new Span<char>(pointer, Length);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref char GetPinnableReference() => ref buffer[0];

        public FixedString.Numerator<FixedString32> GetEnumerator() => new FixedString.Numerator<FixedString32>(ref this);

        #region Object Overrides
        public override int GetHashCode() => FixedString.GetHashCode(ref this);
        public override string ToString() => FixedString.ToString(ref this);

        public override bool Equals(object obj)
        {
            if (obj is IFixedString target)
                return FixedString.Matches(this, target);

            return false;
        }
        #endregion

        #region Equality & Comparison Methods
        public bool Equals(FixedString32 target) => FixedString.Matches(this, target);

        public int CompareTo(FixedString32 target) => FixedString.CompareTo(this, target);
        #endregion

        #region Constructors
        public FixedString32(ReadOnlySpan<char> source)
        {
            if (source.Length > MaxSize)
                throw new InvalidOperationException($"Source Bigger than Max Size of {MaxSize}");

            internal_length = source.Length;

            fixed (char* ptr = buffer)
            {
                var destination = new Span<char>(ptr, internal_length);

                source.CopyTo(destination);
            }
        }

        public FixedString32(int length)
        {
            if (length > MaxSize)
                throw new InvalidOperationException($"Argument Length Bigger than Max Size of {MaxSize}");

            internal_length = length;
        }

        public FixedString32(bool fill)
        {
            if (fill)
                internal_length = MaxSize;
            else
                internal_length = 0;
        }
        #endregion

        #region Equality Operators
        public static bool operator ==(FixedString32 left, FixedString32 right) => FixedString.Matches(left, right);
        public static bool operator !=(FixedString32 left, FixedString32 right) => !FixedString.Matches(left, right);

        public static bool operator ==(FixedString32 left, string right) => FixedString.Matches(left, right);
        public static bool operator !=(FixedString32 left, string right) => !FixedString.Matches(left, right);

        public static bool operator ==(string right, FixedString32 left) => FixedString.Matches(left, right);
        public static bool operator !=(string right, FixedString32 left) => !FixedString.Matches(left, right);
        #endregion

        #region Conversions Operators
        public static implicit operator FixedString32(ReadOnlySpan<char> span) => new FixedString32(span);
        public static implicit operator ReadOnlySpan<char>(FixedString32 span) => span.ToSpan();

        public static implicit operator Span<char>(FixedString32 span) => span.ToSpan();

        public static implicit operator FixedString32(string text) => new FixedString32(text);
        #endregion
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FixedString64 : IFixedString, IEquatable<FixedString64>, IComparable<FixedString64>
    {
        public const int MaxSize = 64;

        fixed char buffer[MaxSize];

        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index of {index} out of Length of {Length}");

                return buffer[index];
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index of {index} out of Length of {Length}");

                buffer[index] = value;
            }
        }

        int internal_length;
        public int Length
        {
            get => internal_length;
            set
            {
                if (internal_length > MaxSize || internal_length < 0)
                    throw new ArgumentOutOfRangeException(nameof(Length));

                internal_length = value;
            }
        }

        public int Capacity => MaxSize;

        public Span<char> ToSpan()
        {
            fixed (char* pointer = buffer)
            {
                return new Span<char>(pointer, Length);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref char GetPinnableReference() => ref buffer[0];

        public FixedString.Numerator<FixedString64> GetEnumerator() => new FixedString.Numerator<FixedString64>(ref this);

        #region Object Overrides
        public override int GetHashCode() => FixedString.GetHashCode(ref this);
        public override string ToString() => FixedString.ToString(ref this);

        public override bool Equals(object obj)
        {
            if (obj is IFixedString target)
                return FixedString.Matches(this, target);

            return false;
        }
        #endregion

        #region Equality & Comparison Methods
        public bool Equals(FixedString64 target) => FixedString.Matches(this, target);

        public int CompareTo(FixedString64 target) => FixedString.CompareTo(this, target);
        #endregion

        #region Constructors
        public FixedString64(ReadOnlySpan<char> source)
        {
            if (source.Length > MaxSize)
                throw new InvalidOperationException($"Source Bigger than Max Size of {MaxSize}");

            internal_length = source.Length;

            fixed (char* ptr = buffer)
            {
                var destination = new Span<char>(ptr, internal_length);

                source.CopyTo(destination);
            }
        }

        public FixedString64(int length)
        {
            if (length > MaxSize)
                throw new InvalidOperationException($"Argument Length Bigger than Max Size of {MaxSize}");

            internal_length = length;
        }

        public FixedString64(bool fill)
        {
            if (fill)
                internal_length = MaxSize;
            else
                internal_length = 0;
        }
        #endregion

        #region Equality Operators
        public static bool operator ==(FixedString64 left, FixedString64 right) => FixedString.Matches(left, right);
        public static bool operator !=(FixedString64 left, FixedString64 right) => !FixedString.Matches(left, right);

        public static bool operator ==(FixedString64 left, string right) => FixedString.Matches(left, right);
        public static bool operator !=(FixedString64 left, string right) => !FixedString.Matches(left, right);

        public static bool operator ==(string right, FixedString64 left) => FixedString.Matches(left, right);
        public static bool operator !=(string right, FixedString64 left) => !FixedString.Matches(left, right);
        #endregion

        #region Conversions Operators
        public static implicit operator FixedString64(ReadOnlySpan<char> span) => new FixedString64(span);
        public static implicit operator ReadOnlySpan<char>(FixedString64 span) => span.ToSpan();

        public static implicit operator Span<char>(FixedString64 span) => span.ToSpan();

        public static implicit operator FixedString64(string text) => new FixedString64(text);
        #endregion
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FixedString128 : IFixedString, IEquatable<FixedString128>, IComparable<FixedString128>
    {
        public const int MaxSize = 128;

        fixed char buffer[MaxSize];

        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index of {index} out of Length of {Length}");

                return buffer[index];
            }
            set
            {
                if (index < 0 || index >= Length)
                    throw new IndexOutOfRangeException($"Index of {index} out of Length of {Length}");

                buffer[index] = value;
            }
        }

        int internal_length;
        public int Length
        {
            get => internal_length;
            set
            {
                if (internal_length > MaxSize || internal_length < 0)
                    throw new ArgumentOutOfRangeException(nameof(Length));

                internal_length = value;
            }
        }

        public int Capacity => MaxSize;

        public Span<char> ToSpan()
        {
            fixed (char* pointer = buffer)
            {
                return new Span<char>(pointer, Length);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref char GetPinnableReference() => ref buffer[0];

        public FixedString.Numerator<FixedString128> GetEnumerator() => new FixedString.Numerator<FixedString128>(ref this);

        #region Object Overrides
        public override int GetHashCode() => FixedString.GetHashCode(ref this);
        public override string ToString() => FixedString.ToString(ref this);

        public override bool Equals(object obj)
        {
            if (obj is IFixedString target)
                return FixedString.Matches(this, target);

            return false;
        }
        #endregion

        #region Equality & Comparison Methods
        public bool Equals(FixedString128 target) => FixedString.Matches(this, target);

        public int CompareTo(FixedString128 target) => FixedString.CompareTo(this, target);
        #endregion

        #region Constructors
        public FixedString128(ReadOnlySpan<char> source)
        {
            if (source.Length > MaxSize)
                throw new InvalidOperationException($"Source Bigger than Max Size of {MaxSize}");

            internal_length = source.Length;

            fixed (char* ptr = buffer)
            {
                var destination = new Span<char>(ptr, internal_length);

                source.CopyTo(destination);
            }
        }

        public FixedString128(int length)
        {
            if (length > MaxSize)
                throw new InvalidOperationException($"Argument Length Bigger than Max Size of {MaxSize}");

            internal_length = length;
        }

        public FixedString128(bool fill)
        {
            if (fill)
                internal_length = MaxSize;
            else
                internal_length = 0;
        }
        #endregion

        #region Equality Operators
        public static bool operator ==(FixedString128 left, FixedString128 right) => FixedString.Matches(left, right);
        public static bool operator !=(FixedString128 left, FixedString128 right) => !FixedString.Matches(left, right);

        public static bool operator ==(FixedString128 left, string right) => FixedString.Matches(left, right);
        public static bool operator !=(FixedString128 left, string right) => !FixedString.Matches(left, right);

        public static bool operator ==(string right, FixedString128 left) => FixedString.Matches(left, right);
        public static bool operator !=(string right, FixedString128 left) => !FixedString.Matches(left, right);
        #endregion

        #region Conversions Operators
        public static implicit operator FixedString128(ReadOnlySpan<char> span) => new FixedString128(span);
        public static implicit operator ReadOnlySpan<char>(FixedString128 span) => span.ToSpan();

        public static implicit operator Span<char>(FixedString128 span) => span.ToSpan();

        public static implicit operator FixedString128(string text) => new FixedString128(text);
        #endregion
    }
    #endregion
}