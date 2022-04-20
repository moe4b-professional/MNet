using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MNet
{
    public unsafe interface IFixedString
    {
        int Length { get; set; }

        int Capacity { get; }

        char this[int index] { get; set; }

        public Span<char> ToSpan();

        ref char GetPinnableReference();
    }

    public static unsafe class FixedString
    {
        public const int MaxSize = 256;

        #region Matches
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Matches<TLeft, TRight>(ref this TLeft left, ref TRight right)
            where TLeft : struct, IFixedString
            where TRight : IFixedString
        {
            return Matches(ref left, ref right, StringComparison.Ordinal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Matches<TLeft, TRight>(ref this TLeft left, ref TRight right, StringComparison comparison)
            where TLeft : struct, IFixedString
            where TRight : IFixedString
        {
            if (left.Length != right.Length)
                return false;

            fixed (char* ptr1 = left, ptr2 = right)
            {
                var span1 = new Span<char>(ptr1, left.Length);
                var span2 = new Span<char>(ptr2, right.Length);

                return MemoryExtensions.Equals(span1, span2, comparison);
            }
        }
        #endregion

        #region Compare
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CompareTo<TLeft, TRight>(ref this TRight left, ref TLeft right)
            where TLeft : struct, IFixedString
            where TRight : struct, IFixedString
        {
            return CompareTo(ref left, ref right, StringComparison.Ordinal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CompareTo<TLeft, TRight>(ref this TRight left, ref TLeft right, StringComparison comparison)
            where TLeft : struct, IFixedString
            where TRight : struct, IFixedString
        {
            fixed (char* ptr1 = left, ptr2 = right)
            {
                var span1 = new Span<char>(ptr1, left.Length);
                var span2 = new Span<char>(ptr2, right.Length);

                return MemoryExtensions.CompareTo(span1, span2, comparison);
            }
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
            fixed (char* ptr = target)
            {
                var span = new Span<char>(ptr, target.Length);
                return new string(span);
            }
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assign<T>(ref this T target, ReadOnlySpan<char> source)
            where T : struct, IFixedString
        {
            if (source.Length > target.Capacity)
                throw new InvalidOperationException($"Source Bigger than Capacity of {target.Capacity}");

            target.Length = source.Length;

            fixed (char* ptr = target)
            {
                var destination = new Span<char>(ptr, target.Length);
                source.CopyTo(destination);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Clone<T>(this T target)
            where T : struct, IFixedString
        {
            return target;
        }

        #region Iteration
        public static Numerator<T> GetEnumerator<T>(this T target)
            where T : IFixedString
        {
            return new Numerator<T>(target);
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

            public Numerator(T text)
            {
                this.Text = text;
                index = -1;
            }
        }
        #endregion
    }

    #region Variants
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FixedString16 : IFixedString, IEquatable<FixedString16>, IComparable<FixedString16>
    {
        public const int Size = 16;

        fixed char buffer[Size];

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

        public int Length { get; set; }

        public int Capacity => Size;

        public Span<char> ToSpan()
        {
            fixed (char* pointer = buffer)
            {
                return new Span<char>(pointer, Length);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref char GetPinnableReference() => ref buffer[0];

        #region Overrides
        public override int GetHashCode() => FixedString.GetHashCode(ref this);
        public override string ToString() => FixedString.ToString(ref this);

        public override bool Equals(object obj)
        {
            if (obj is IFixedString target)
                return FixedString.Matches(ref this, ref target);

            return false;
        }
        #endregion

        #region Equality & Comparison
        public bool Equals(FixedString16 target) => FixedString.Matches(ref this, ref target);

        public int CompareTo(FixedString16 target) => FixedString.CompareTo(ref this, ref target);
        #endregion

        #region Constructors
        public FixedString16(ReadOnlySpan<char> source)
        {
            Length = default;

            FixedString.Assign(ref this, source);
        }

        public FixedString16(int length)
        {
            this.Length = length;
        }

        public FixedString16(bool fill)
        {
            if (fill)
                Length = Size;
            else
                Length = 0;
        }
        #endregion

        public static bool operator ==(FixedString16 left, FixedString16 right) => left.Equals(right);
        public static bool operator !=(FixedString16 left, FixedString16 right) => !left.Equals(right);
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FixedString32 : IFixedString, IEquatable<FixedString32>, IComparable<FixedString32>
    {
        public const int Size = 32;

        fixed char buffer[Size];

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

        public int Length { get; set; }

        public int Capacity => Size;

        public Span<char> ToSpan()
        {
            fixed (char* pointer = buffer)
            {
                return new Span<char>(pointer, Length);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref char GetPinnableReference() => ref buffer[0];

        #region Overrides
        public override int GetHashCode() => FixedString.GetHashCode(ref this);
        public override string ToString() => FixedString.ToString(ref this);

        public override bool Equals(object obj)
        {
            if (obj is IFixedString target)
                return FixedString.Matches(ref this, ref target);

            return false;
        }
        #endregion

        #region Equality & Comparison
        public bool Equals(FixedString32 target) => FixedString.Matches(ref this, ref target);

        public int CompareTo(FixedString32 target) => FixedString.CompareTo(ref this, ref target);
        #endregion

        #region Constructors
        public FixedString32(ReadOnlySpan<char> source)
        {
            Length = default;

            FixedString.Assign(ref this, source);
        }

        public FixedString32(int length)
        {
            this.Length = length;
        }

        public FixedString32(bool fill)
        {
            if (fill)
                Length = Size;
            else
                Length = 0;
        }
        #endregion

        public static bool operator ==(FixedString32 left, FixedString32 right) => left.Equals(right);
        public static bool operator !=(FixedString32 left, FixedString32 right) => !left.Equals(right);
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FixedString64 : IFixedString, IEquatable<FixedString64>, IComparable<FixedString64>
    {
        public const int Size = 64;

        fixed char buffer[Size];

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

        public int Length { get; set; }

        public int Capacity => Size;

        public Span<char> ToSpan()
        {
            fixed (char* pointer = buffer)
            {
                return new Span<char>(pointer, Length);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref char GetPinnableReference() => ref buffer[0];

        #region Overrides
        public override int GetHashCode() => FixedString.GetHashCode(ref this);
        public override string ToString() => FixedString.ToString(ref this);

        public override bool Equals(object obj)
        {
            if (obj is IFixedString target)
                return FixedString.Matches(ref this, ref target);

            return false;
        }
        #endregion

        #region Equals
        public bool Equals(FixedString64 target) => FixedString.Matches(ref this, ref target);

        public bool Equals(FixedString64 target, StringComparison comparison) => FixedString.Matches(ref this, ref target, comparison);
        #endregion

        #region Compare To
        public int CompareTo(FixedString64 target) => FixedString.CompareTo(ref this, ref target);

        public int CompareTo(FixedString64 target, StringComparison comparison) => FixedString.CompareTo(ref this, ref target, comparison);
        #endregion

        #region Constructors
        public FixedString64(ReadOnlySpan<char> source)
        {
            Length = default;

            FixedString.Assign(ref this, source);
        }

        public FixedString64(int length)
        {
            this.Length = length;
        }

        public FixedString64(bool fill)
        {
            if (fill)
                Length = Size;
            else
                Length = 0;
        }
        #endregion

        public static bool operator ==(FixedString64 left, FixedString64 right) => left.Equals(right);
        public static bool operator !=(FixedString64 left, FixedString64 right) => !left.Equals(right);
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FixedString128 : IFixedString, IEquatable<FixedString128>, IComparable<FixedString128>
    {
        public const int Size = 128;

        fixed char buffer[Size];

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

        public int Length { get; set; }

        public int Capacity => Size;

        public Span<char> ToSpan()
        {
            fixed (char* pointer = buffer)
            {
                return new Span<char>(pointer, Length);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref char GetPinnableReference() => ref buffer[0];

        #region Overrides
        public override int GetHashCode() => FixedString.GetHashCode(ref this);
        public override string ToString() => FixedString.ToString(ref this);

        public override bool Equals(object obj)
        {
            if (obj is IFixedString target)
                return FixedString.Matches(ref this, ref target);

            return false;
        }
        #endregion

        #region Equals
        public bool Equals(FixedString128 target) => FixedString.Matches(ref this, ref target);

        public bool Equals(FixedString128 target, StringComparison comparison) => FixedString.Matches(ref this, ref target, comparison);
        #endregion

        #region Compare To
        public int CompareTo(FixedString128 target) => FixedString.CompareTo(ref this, ref target);

        public int CompareTo(FixedString128 target, StringComparison comparison) => FixedString.CompareTo(ref this, ref target, comparison);
        #endregion

        #region Constructors
        public FixedString128(ReadOnlySpan<char> source)
        {
            Length = default;

            FixedString.Assign(ref this, source);
        }

        public FixedString128(int length)
        {
            this.Length = length;
        }

        public FixedString128(bool fill)
        {
            if (fill)
                Length = Size;
            else
                Length = 0;
        }
        #endregion

        public static bool operator ==(FixedString128 left, FixedString128 right) => left.Equals(right);
        public static bool operator !=(FixedString128 left, FixedString128 right) => !left.Equals(right);
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FixedString256 : IFixedString, IEquatable<FixedString256>, IComparable<FixedString256>
    {
        public const int Size = 256;

        fixed char buffer[Size];

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

        public int Length { get; set; }

        public int Capacity => Size;

        public Span<char> ToSpan()
        {
            fixed (char* pointer = buffer)
            {
                return new Span<char>(pointer, Length);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref char GetPinnableReference() => ref buffer[0];

        #region Overrides
        public override int GetHashCode() => FixedString.GetHashCode(ref this);
        public override string ToString() => FixedString.ToString(ref this);

        public override bool Equals(object obj)
        {
            if (obj is IFixedString target)
                return FixedString.Matches(ref this, ref target);

            return false;
        }
        #endregion

        #region Equals
        public bool Equals(FixedString256 target) => FixedString.Matches(ref this, ref target);

        public bool Equals(FixedString256 target, StringComparison comparison) => FixedString.Matches(ref this, ref target, comparison);
        #endregion

        #region Compare To
        public int CompareTo(FixedString256 target) => FixedString.CompareTo(ref this, ref target);

        public int CompareTo(FixedString256 target, StringComparison comparison) => FixedString.CompareTo(ref this, ref target, comparison);
        #endregion

        #region Constructors
        public FixedString256(ReadOnlySpan<char> source)
        {
            Length = default;

            FixedString.Assign(ref this, source);
        }

        public FixedString256(int length)
        {
            this.Length = length;
        }

        public FixedString256(bool fill)
        {
            if (fill)
                Length = Size;
            else
                Length = 0;
        }
        #endregion

        public static bool operator ==(FixedString256 left, FixedString256 right) => left.Equals(right);
        public static bool operator !=(FixedString256 left, FixedString256 right) => !left.Equals(right);
    }
    #endregion
}